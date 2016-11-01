<h1>Architecture</h1>

![Adding your SQL server and database](https://github.com/Azure/etlorchestration-cortana-intelligence-preconfigured-solution/blob/master/Docs/figures/Architecture.png)

<h1>Visualize Using Power BI</h1>
The generated Fact and Dimension tables can be visualized in Power BI by connecting to the SQL Data Warehouse instance. Refer [this sample Power BI Desktop file](https://github.com/Azure/etlorchestration-cortana-intelligence-preconfigured-solution/tree/master/Power-BI-Templates/AzureEtlOrchestrationSampleDashboard.pbix). See [PBI section](#pbi-setup) for details on wiring it up with your Data Warehouse instance. 

#### Power BI Dashboard <a id="pbi-setup"/>

Power BI can connect to our data mart hosted on Azure SQL Data Warehouse to visualize the generated Facts and Dimensions. This section describes how to set up the sample Power BI dashboard to visualize the results of the pipeline.

1) Get the database server name, database name, user name and password from the [deployment summary page](https://start.cortanaintelligence.com/Deployments?type=avhivedw) on CIS.

![SQL Database credentials in deployment summary page](https://github.com/Azure/etlorchestration-cortana-intelligence-preconfigured-solution/blob/master/Docs/figures/SqlServerCredentials.png)

2) Update the data source of the Power BI file.
 - Make sure you have the latest version of [Power BI desktop](https://powerbi.microsoft.com/desktop) installed.
 - Download the [Power BI template](https://github.com/Azure/etlorchestration-cortana-intelligence-preconfigured-solution/tree/master/Power-BI-Templates/AzureEtlOrchestrationSampleDashboard.pbix) for the solution. 
 - The initial visualizations are based on sample data. **Note:** If you see an error message, please make sure you have the latest version of Power BI Desktop installed.
 Click **‘Edit Queries’** and choose **‘Data Source Settings’** from the menu.

 ![Changing datasource in Power BI](https://github.com/Azure/etlorchestration-cortana-intelligence-preconfigured-solution/blob/master/Docs/figures/PBI_DataSource_settings.png)
 - The resulting dialog will show SQL Server which is queried to fetch data for Power BI dashboard. Click the **‘Change Source...’** button and replace **‘Server’** and **‘Database’** settings in the resulting dialog with your own server and database names from step 1. Click **‘OK’**.
 
![Adding your SQL server and database](https://github.com/Azure/etlorchestration-cortana-intelligence-preconfigured-solution/blob/master/Docs/figures/PBI_DataSource_dialog.png)
   
- Click **‘Close’** to exit the **‘Data Source Settings’** dialog. A warning will appear prompting you to apply the changes. Click the **‘Apply Changes’** button.
   
![PBI warning message prompting the user to apply Data Source changes](https://github.com/Azure/etlorchestration-cortana-intelligence-preconfigured-solution/blob/master/Docs/figures/PBI_update_ribbon.png)
   
- A dialog prompting the user for Database credentials will appear. Click **‘Database’**, fill in your **‘Username’** and **‘Password’** from step 1. Then click **‘Connect’**.
   
![Database credentials prompt](https://github.com/Azure/etlorchestration-cortana-intelligence-preconfigured-solution/blob/master/Docs/figures/PBI_SqlServerUsernamePassword_prompt.png)
   
- Save the dashboard. Your Power BI file now has an established connection to the server. If your visualizations are empty, make sure you clear the selections on the visualizations to visualize all the data by clicking the eraser icon on the upper right corner of the legends. Use the refresh button to reflect new data on the visualizations. 

3) (Optional) Publish the dashboard to [Power BI online](http://www.powerbi.com/). Note that this step needs a Power BI account (or Office 365 account).
- Click **‘Publish’** and few seconds later a window appears displaying "Publishing to Power BI Success!" with a green check mark. To find detailed instructions, see [Publish from Power BI Desktop](https://support.powerbi.com/knowledgebase/articles/461278-publish-from-power-bi-desktop).
- To create a new dashboard: click the + sign next to the **Dashboards** section on the left pane. Enter the name "IT Anomaly Insights" for this new dashboard.
 
4) (Optional) Schedule refresh of the data source.
- To schedule refresh of the data, hover your mouse over the dataset, click "..." and then choose **Schedule Refresh**. **Note:** If you see a warning massage, click **Edit Credentials** and make sure your database credentials are the same as those described in step 1.
- Expand the **Schedule Refresh** section. Turn on "keep your data up-to-date". 
- Schedule the refresh based on your needs. To find more information, see [Data refresh in Power BI](https://powerbi.microsoft.com/documentation/powerbi-refresh-data/).

<h1>Under The Hood</h1>
<h2 align="center">BATCH LOADS</h2>
### Generate Dimension
#### Create Dimension Table
We begin by creating a Hive external table for each OLTP source table which has been cloned to Azure Blob (our data 'lake'). These source tables are writted out into tsv files partitioned by timestamp. We also create the dimension table as a transactional Hive table. We choose to keep this transactional to enable updates & deletes against this.

Dimension tables each include a created_on & last_modified column to represent dimension creation and last-updated timestamps. All dimension tables are clustered by the issued surrogate key.

Below, we create external tables **currency** (OLTP Source Table) which will be used as a sources table to generate our **dim_currency** table. 

```sql
DROP TABLE IF EXISTS currency;
CREATE EXTERNAL TABLE currency (
	CurrencyCode      string,
	Name                  string,
	ModifiedDate          string)
ROW FORMAT DELIMITED FIELDS TERMINATED BY '\t'
LOCATION 'wasb://data@{0}.blob.core.windows.net/Sales.Currency/';

DROP TABLE IF EXISTS dim_currency PURGE;
CREATE TABLE dim_currency (
    CurrencyKey int,
    CurrencyAlternateKey string, 
    CurrencyName string,
    CreationOn timestamp,
    LastModified timestamp)
CLUSTERED BY (CurrencyKey) INTO 32 BUCKETS
STORED AS ORC
LOCATION 'wasb://data@{0}.blob.core.windows.net/DimCurrency/'
TBLPROPERTIES ('transactional'='true');
```
#### Create Index on Dimension's Natural Key
Additionally, a hive index is created against the natural key for each dimension. This is done to improve join performance when attempting to determine candidates for insertion and update while processing incremental slices. Here we create an index on **CurrencyAlternateKey** (dim_currency's natural key).

```sql
CREATE INDEX CurrencyAlternateKey_index 
ON TABLE dim_currency (CurrencyAlternateKey) 
AS 'COMPACT' WITH DEFERRED REBUILD;
```

#### Batch Insert Dimension Data
Next, we write the data dimension data to the internal table.

```sql
INSERT OVERWRITE TABLE dim_currency 
SELECT 
    ROW_NUMBER() OVER () AS CurrencyKey,
    CurrencyCode AS CurrencyAlternateKey, 
    Name AS CurrencyName,
    current_timestamp AS CreationOn,
    current_timestamp AS LastModified
FROM currency;
```

### Generate Fact
In a manner similar to the steps taken to ingest dimensions, we create external Hive tables for each dependant OLTP source. A Fact is typically generated off one or more dependant dimensions and OLTP source tables. Fact tables are also created as transactional tables to enable incremental updates/deletes. In addition, created_on & last_modified columns are added and tables are clustered by their logical key. 

Below, we create external tables **DimDate** (Date Dimension) & **CurrencyRate** (OLTP Source Table) which will be used as sources tables to generate our **fact_currency_rate** table. 

```sql
CREATE EXTERNAL TABLE IF NOT EXISTS DimDate (
    DateKey int, 
    FullDateAlternateKey date, 
    DayNumberOfWeek smallint, 
    EnglishDayNameOfWeek string, 
    SpanishDayNameOfWeek string, 
    FrenchDayNameOfWeek string, 
    DayNumberOfMonth smallint, 
    DayNumberOfYear int, 
    WeekNumberOfYear smallint, 
    EnglishMonthName string, 
    SpanishMonthName string, 
    FrenchMonthName string, 
    MonthNumberOfYear smallint, 
    CalendarQuarter smallint, 
    CalendarYear int, 
    CalendarSemester smallint, 
    FiscalQuarter smallint, 
    FiscalYear int, 
    FiscalSemester smallint
)
ROW FORMAT DELIMITED FIELDS TERMINATED BY '\t'
LOCATION 'wasb://data@{0}.blob.core.windows.net/DimDate/';

DROP TABLE IF EXISTS CurrencyRate;
CREATE EXTERNAL TABLE CurrencyRate (
    CurrencyRateID   int,
    CurrencyRateDate timestamp,
    FromCurrencyCode string,
    ToCurrencyCode   string,
    AverageRate      decimal(19,8),
    EndOfDayRate     decimal(19,8),
    ModifiedDate     timestamp)
ROW FORMAT DELIMITED FIELDS TERMINATED BY '\t'
LOCATION 'wasb://data@{0}.blob.core.windows.net/Sales.CurrencyRate/';

DROP TABLE IF EXISTS fact_currency_rate PURGE;
CREATE TABLE fact_currency_rate (
    CurrencyKey      int,
    TimeKey          int,
    AverageRate  decimal(19,8),
    EndOfDayRate decimal(19,8),
    CreationOn  timestamp,
    LastModified timestamp
)
CLUSTERED BY (CurrencyKey, TimeKey) INTO 32 BUCKETS
STORED AS ORC
LOCATION 'wasb://data@{0}.blob.core.windows.net/FactCurrencyRate/'
TBLPROPERTIES ('transactional'='true');
```

#### Batch Insert Fact Data
Next, we write the Fact data to the internal table.

```sql
INSERT OVERWRITE TABLE fact_currency_rate
SELECT 
    dc.CurrencyKey AS CurrencyKey, 
    dt.DateKey AS TimeKey, 
    cast((1.00000000 / cr.AverageRate) as decimal(19,8)) AS AverageRate, 
    cast((1.00000000 / cr.EndOfDayRate) as decimal(19,8)) AS EndOfDayRate,
    current_timestamp AS CreationOn,
    current_timestamp AS LastModified
FROM CurrencyRate AS cr 
    INNER JOIN DimDate AS dt 
        ON cast(cr.CurrencyRateDate as date) = dt.FullDateAlternateKey
    INNER JOIN dim_currency AS dc 
        ON cr.ToCurrencyCode = dc.CurrencyAlternateKey;
```

<h2 align="center">INCREMENTAL LOADS</h2>
## Dimension
We follow the Type 1 model of Change Data Capture (CDC) for our slowly changing dimensions (SCD). Particularly, we do not track historical data, and proceed to overwrite existing records on updates. Hive which is used as our data lake store does not currently support sub-query based updates. So, we proceed with a delete and insert on rows to be updated. Update candidates are filtered out as follows:
-	Incrementals enter the partitioned blob store.
-	A âreconcile viewâ is generated against the partitioned store for each source table.
-	The dimension is generated from the reconcile views and rows modified before the update interval are filtered out.
-   The candidates are then joined with the dimension table to differentiate update vs. insert candidates. 

#### Generate Reconcile Views
The 'Reconcile View' of an OLTP source table combines and reduces the source table to show only the most recent records based on the last_modified timestamp. Row reconciliation is performed by grouping rows by the primary key of the source table and selecting the record with the most recent modification.

```sql
-- Let T be an OLTP source table.
-- Let pK be the primary key of this table.

CREATE VIEW reconcile_view
AS SELECT T.*
FROM T JOIN
(SELECT pK, max(last_modified) AS max_modified FROM T GROUP BY pK) S
ON T.pK = S.pK 
AND T.last_modified = S.max_modified;
```

#### Determine Time-Slice Delta Rows
We proceed to determine the rows to be inserted/updated on the dimension. To achieve this, we perform the join operation originally used to construct the dimension using the most 'recent view' (as conveyed by the 'reconcile views') of our source tables. We then, filter out rows where any dependant source table's last_modified timestamp falls within the time-slice under consideration. i.e. 

```sql
-- Let S1,S2,..Sn be source OLTP tables generating dimension D.
-- Time slice under consideration is (slice_start, slice_end).

CREATE VIEW slice_delta_rows
<dimension-generating-subquery>
WHERE S1.last_modified BETWEEN slice_start AND slice_end
OR S2.last_modified BETWEEN slice_start AND slice_end
OR S3.last_modified BETWEEN slice_start AND slice_end
..
OR Sn.last_modified BETWEEN slice_start AND slice_end;
```

#### Isolate Candidate Rows for Update & Insert
Next, we join the slice_delta_rows with the dimension table to determine which rows require update. Particularly, we are looking for pre-existing surrogate key ids for rows in the delta rowset. Since surrogate keys is not issued yet for the slice_delta_rows, the tables are joined by the dimension's predefined natural key.

```sql
-- Let D be the dimension under consideration.
-- Natural key for D is *nK*.
-- Assigned Surrogate key for D is *sK*.

CREATE VIEW update_insert_candidates
SELECT D.sK AS id, D.created_on, sdr.*
FROM D RIGHT JOIN slice_delta_rows AS sdr
ON D.nK = sdr.nK;
```

#### Delete Update Candidates For Later Insert.
We now deleterows that are to be updated using the ids retrieved from the previous join.

```sql
-- Let D be the dimension under consideration.
-- Assigned Surrogate key for D is *sK*.

DELETE 
FROM D
WHERE sK IN 
(SELECT id from update_insert_candidates WHERE id IS NOT NULL);
```

#### Insert Candidates
Finally, rows are inserted into the Dimension table. Surrogate keys are generated on insertion manually using row numbers.

```sql
-- Let D be the dimension under consideration.
-- Assigned Surrogate key for D is *sK*.

CREATE VIEW d_max_id AS
SELECT COALESCE(MAX(sK),0) AS maxid 
FROM D;

INSERT INTO D 
    COALESCE(uc.id ,d_max_id.maxid + ROW_NUMBER() OVER (ORDER BY maxid)) AS sK,
    ..
    ..
    COALESCE(uc.created_on, current_timestamp) AS CreationOn,
    current_timestamp AS LastModified
FROM update_insert_candidates AS uc CROSS JOIN d_max_id;
```