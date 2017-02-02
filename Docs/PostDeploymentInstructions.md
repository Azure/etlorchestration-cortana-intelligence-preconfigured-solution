<h1 id="overview">Overview</h1>
The following document describes details on the **Extract-Transform-Load On Azure Solution** deployed via [*Cortana Intelligence Solutions (CIS)*](https://start.cortanaintelligence.com/). It covers the following: 

  * [Architecture](#architecture): A high level description of deployed components, building-blocks and resulting outputs. 
  * [Data Flow](#dataflow): Describes the datasets created and transforms applied over various services to generate the star-schema model of the source OLTP data.
  * [Dataset](#datasets): Overview of the Adventure Works OLTP dataset: our source OLTP database for this solution.
  * [Monitoring](#monitor): Details on monitoring and setting up alarms for your warehousing pipeline.
  * [Visualizing with Power BI](#pbi-setup): A wallk through on sourcing the OLAP data to visualize a sample *Reseller Sales Dashboard* using Power BI.
  * [Batch Load and Incremental Processing](#under-the-hood): Covers the details of Hive queries used, tables created and the procedures applied to perform the initial load and ingest incrementals to support change data capture for the dimensions and facts.

<h1 id="architecture">Architecture</h1>
In this solution, we demonstrate how a hybrid EDW scenario can be implemented on Azure using: 
* **Azure SQL Data Warehouse** as a Data mart to vend business-line specific data.
* **Azure Analysis Services** as an analytical engine to drive reporting.
* **Azure Blob Storage** as a Data Lake to store raw data in their native format until needed in a flat architecture. 
* **Azure HDInsight** as a processing layer to transform, sanitize and load raw data into a de-normalized format suitable for analytics. 
* **Azure Data Factory** as our orchestration engine to move, transform, monitor and process data in a scheduled time-slice based manner. 

Our scenario includes an Extract-Load-and-Transform (ELT) model. Firstly, we extract data from an operational OLTP data source into Azure Blob Storage. Azure Blob acts as landing zone to process initially loaded data. We then transform the data to generate facts and dimensions using Azure HDInsight's Hive as our processing engine. This processed data is then moved into Azure SQL Data Warehouse that acts as data mart for reporting and analysis. We then show how this data can be visualized on tools such as PowerBI. Importantly, we also show how this entire architecture can be orchestrated and monitored through Azure Data Factory. To demonstrate this, we deploy both a batch pipeline to showcase initial bulk data load and an incremental pipeline to instrument change data capture for incoming data slices. 

![High Level Pipeline Architecture](https://github.com/Azure/etlorchestration-cortana-intelligence-preconfigured-solution/blob/master/Docs/figures/ArchitectureDiagram.png)

<h1 id="dataflow">Data Flow</h1>
The following steps are performed as outlined in the chart above: 
* **[1->2]** Normalized OLTP data is cloned to Azure Blob storage every hour. Data copied is partitioned by time slice at a 1 hour granularity.
* **[3->4->5]** Hive external tables are created against the cloned source OLTP data and used to generate dimensions which are writtern back to a Hive transactional table. Refer [here](#batch-loads) for details of the transforms applied. In the incremental pipeline, deltas are reconciled using the procedure outlined [here](#incremental-loads).
* **[5->6->7]** Generated dimensions and source OLTP data are used to generate Hive transactional Fact tables.
* **[6->7/8->9]** Fact & Dimension tables are written out to CSV files in Azure Blob to enable Polybase load into the data mart (Azure SQL Data Warehouse). Stored procedure activities in Azure Data Factory are kicked off to load external tables and subsequent inserts into Fact and Dimension tables. In the incremental pipeline, deltas are reconciled in a manner similar to the procedure outlined [here](#incremental-loads).
* **[10]** Data  sourced from the data mart is used to visualize dashboards referencing the OLAP models generated.

![Pipeline Data Flow Chart](https://github.com/Azure/etlorchestration-cortana-intelligence-preconfigured-solution/blob/master/Docs/figures/FlowChartDiagram.png)

<h1 id="datasets">Dataset</h1>
The data used as our OLTP source models a fictious company named 'Adventure Works Cycles'; a large, multinational manufacturing company. The company manufactures and sells metal and composite bicycles to North American, European and Asian commercial markets. Refer [here](https://technet.microsoft.com/en-us/library/ms124825(v=sql.100).aspx) for deeper look at the various business scenarios addressed by this dataset.

### Synthetic Data Generation 
To simulate incremental inserts, we deploy a data generator to simulate sales orders being produced in real-time. This will be deployed as a webjob in your subscription as part of the solution. You can view the status of this webjob by referring to the [CIS deployment page](https://start.cortanaintelligence.com/Deployments?type=avhivedw). You can also view the [source code here](https://github.com/Azure/etlorchestration-cortana-intelligence-preconfigured-solution/tree/master/Src/SalesDataGenerator/DataGenerator).

![Dataset Figure 1](https://github.com/Azure/etlorchestration-cortana-intelligence-preconfigured-solution/blob/master/Docs/figures/DATASET-1.png)

<h1 id="monitor">Monitoring your Warehousing Pipeline</h1>

### Reviewing Fact/Dimension Generation Health

1) Head to the **Monitor & Manage App** from the Azure Data Factory blade. You can access this blade quickly by referencing your [CIS deployment page](https://start.cortanaintelligence.com/Deployments?type=avhivedw) once the deployment is complete. The data factory blade is accessible by clicking the **Azure Data Factory** link on this page. 

![Monitoring Figure 1](https://github.com/Azure/etlorchestration-cortana-intelligence-preconfigured-solution/blob/master/Docs/figures/MON-1.png)

2) Because we simulate our batch pipeline as being a one-time initial load in the past, set the *Start Time (UTC)* and *End time (UTC)* to ```06/10/2016 12:00 am``` and ```06/15/2016 12:00 am``` to be able to view activity windows for the batch pipeline.

![Monitoring Figure 2](https://github.com/Azure/etlorchestration-cortana-intelligence-preconfigured-solution/blob/master/Docs/figures/MON-2.png)

3) Notice now that the ```Activity Windows``` pane shows several time slices in various stages (Read, In-Progress, Waiting etc.). To view the slices from the **Batch pipeline**, click the funnel icon next to **Pipeline** and select the **BatchLoadPipeline**.

![Monitoring Figure 3](https://github.com/Azure/etlorchestration-cortana-intelligence-preconfigured-solution/blob/master/Docs/figures/MON-3.png)

4) To view the status of the **Dimensions** generated, click the funnel icon next to **Activity** and key in **Dim**. Next, select all the dimensions that you would like to view the status for. These may be one of *DimProduct*, *DimCurrency* and *DimEmployee*.

![Monitoring Figure 4](https://github.com/Azure/etlorchestration-cortana-intelligence-preconfigured-solution/blob/master/Docs/figures/MON-4.png)
  
5) Similarly, to view the status of the **Facts** generated, click the funnel icon next to **Activity** and key in **Fact**. Next, select all facts that you wish to view the status for. These may be one of *FactSalesQuota*, *FactCurrencyRate* and *FactResellerSales*.

![Monitoring Figure 5](https://github.com/Azure/etlorchestration-cortana-intelligence-preconfigured-solution/blob/master/Docs/figures/MON-5.png)

6) You can even drill on a specific time slice by setting the **Window Start** and **Window End** filters to a time range enclosing the required slices. 

![Monitoring Figure 6](https://github.com/Azure/etlorchestration-cortana-intelligence-preconfigured-solution/blob/master/Docs/figures/MON-6.png)

7) Selecting the activity window, displays an **Activity Window Explorer** pane on the right, which shows the events that have succeded/failed on a calendar marked in green/red. It also shows details of the dependant slices and logs tied to the activity. This provides a quick stop place to drill down on activity failures and gain quick access to your **failure logs**. 

![Monitoring Figure 7](https://github.com/Azure/etlorchestration-cortana-intelligence-preconfigured-solution/blob/master/Docs/figures/MON-7.png)
![Monitoring Figure 8](https://github.com/Azure/etlorchestration-cortana-intelligence-preconfigured-solution/blob/master/Docs/figures/MON-8.png)

8) Similarly to track fact/dimension generation in the incrementals pipeline, set the **Pipeline** filter to **IncrementalSyncPipeline**. 
  * For Facts, set the search filter under **Activity** to **Fact** and select any/all of the following: *FactSalesQuota-Inc*, *FactCurrencyRate-Inc* and *FactResellerSales-Inc*.
  * For Dimensions, set the search filter under **Activity** to **Dimension** and select any/all of the following: *DimCurrency-Inc*.

### Setting Up Alerts 
You can set up alarms to send email notifications when pipeline activities fail for whatever reason. The steps to set this up are as follows:

1) Begin by heading over to the Alerts tab on the Monitor and manage app.

![Alerts Figure 1](https://github.com/Azure/etlorchestration-cortana-intelligence-preconfigured-solution/blob/master/Docs/figures/ALERT-1.png)

2) Click **Add Alert** to add an alert. Give it a suitable *name* and *description*.

![Alerts Figure 2](https://github.com/Azure/etlorchestration-cortana-intelligence-preconfigured-solution/blob/master/Docs/figures/ALERT-2.png)

3) Select a suitable *event*, *status* and *sub-status*. This is typically *Activity Run Finished* and *Failed* to track all failed slices.  

![Alerts Figure 3](https://github.com/Azure/etlorchestration-cortana-intelligence-preconfigured-solution/blob/master/Docs/figures/ALERT-3.png)

4) Specify an email address to notify.  

![Alerts Figure 4](https://github.com/Azure/etlorchestration-cortana-intelligence-preconfigured-solution/blob/master/Docs/figures/ALERT-4.png)

<h1>Visualize Using Power BI</h1>
The generated Fact and Dimension tables can be visualized in Power BI by connecting to the SQL Data Warehouse instance. Refer [this sample Power BI Desktop file](https://github.com/Azure/etlorchestration-cortana-intelligence-preconfigured-solution/tree/master/Power-BI-Templates/AzureEtlOrchestrationSampleDashboard.pbix). See [PBI section](#pbi-setup) for details on wiring it up with your Data Warehouse instance. 

<h1 id="pbi-setup">Power BI Dashboard </h1>

Power BI can connect to our data mart hosted on Azure SQL Data Warehouse to visualize the generated Facts and Dimensions. This section describes how to set up the sample Power BI dashboard to visualize the results of the pipeline.

1) Get the database server name & database model name from the [deployment summary page](https://start.cortanaintelligence.com/Deployments?type=avhivedw) on CIS.

2) Update the data source of the Power BI file.
 - Make sure you have the latest version of [Power BI desktop](https://powerbi.microsoft.com/desktop) installed.
 - Download the [Power BI template](https://github.com/Azure/etlorchestration-cortana-intelligence-preconfigured-solution/tree/master/Power-BI-Templates/aasdashboard.pbix) for the solution. 
 - The initial visualizations are based on sample data. **Note:** If you see an error message, please make sure you have the latest version of Power BI Desktop installed.
- Go to ```Edit Queries```->```Data Source Settings```. Set the **Server** and **Database** to the parameters specified in your CIQS deployment page. They should look something like this;
	```
	Server: asazure://aspaaseastus2.asazure.windows.net/<asName>
	Database: SemanticModel
	```
- Under your Azure AS instance, select ```Model``` and hit ```Ok```.
- In case the data fails to load, hit ```Refresh``` on the panel above.
- Save the dashboard. Your Power BI file now has an established connection to the server. If your visualizations are empty, make sure you clear the selections on the visualizations to visualize all the data by clicking the eraser icon on the upper right corner of the legends. Use the refresh button to reflect new data on the visualizations. 

3) (Optional) Publish the dashboard to [Power BI online](http://www.powerbi.com/). Note that this step needs a Power BI account (or Office 365 account).
- Click **‘Publish’** and few seconds later a window appears displaying "Publishing to Power BI Success!" with a green check mark. To find detailed instructions, see [Publish from Power BI Desktop](https://support.powerbi.com/knowledgebase/articles/461278-publish-from-power-bi-desktop).
- To create a new dashboard: click the + sign next to the **Dashboards** section on the left pane. Enter the name "IT Anomaly Insights" for this new dashboard.
 
4) (Optional) Schedule refresh of the data source.
- To schedule refresh of the data, hover your mouse over the dataset, click "..." and then choose **Schedule Refresh**. **Note:** If you see a warning massage, click **Edit Credentials** and make sure your database credentials are the same as those described in step 1.
- Expand the **Schedule Refresh** section. Turn on "keep your data up-to-date". 
- Schedule the refresh based on your needs. To find more information, see [Data refresh in Power BI](https://powerbi.microsoft.com/documentation/powerbi-refresh-data/).

<h1>Under The Hood</h1>
<h2 id="batch-loads" align="center">BATCH LOADS</h2>

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

<h2 id="incremental-loads" align="center">INCREMENTAL LOADS</h2>
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
