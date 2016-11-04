// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AdventureWorksModel.cs" company="Microsoft">
//   Copyright (c) Microsoft.  All rights reserved.
// </copyright>
// <summary>
//   Adventure Works SQL Database OLTP Model.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DataGenerator
{
    using System;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using BackoffPolicies;
    using DataGenerators;

    internal class Program
    {
        private static readonly string SqlServer = ConfigurationManager.AppSettings["SqlDbServerName"];
        private static readonly string SqlUser = ConfigurationManager.AppSettings["SqlUser"];
        private static readonly string SqlPassword = ConfigurationManager.AppSettings["SqlPassword"];

        private static readonly string DisableFilePath =
            Environment.ExpandEnvironmentVariables(
                "%WEBROOT_PATH%\\app_data\\jobs\\continuous\\%WEBJOBS_NAME%\\disable.job");

        /// <summary>
        ///     User's sql database connection string.
        /// </summary>
        private static readonly string SqlDwConnectionString = $"data source=etlupdatedkwow6bo27zvssrv.database.windows.net;initial catalog=AdventureWorks2012;persist security info=True;user id=username;password=Pa$$w0rd12!;MultipleActiveResultSets=True;App=EntityFramework";
        //$"data source={SqlServer}.database.windows.net;initial catalog=AdventureWorks2012;persist security info=True;user id={SqlUser};password={SqlPassword};MultipleActiveResultSets=True;App=EntityFramework";

        private static void Main(string[] args)
        {
            const int backoffMillisecondTimeout = 5000;
            var dataGenerator = new ResellerSaleDataGenerator(SqlDwConnectionString);
            var backoffPolicy = new FixedBackoffPolicy(backoffMillisecondTimeout);
            var dataGeneratorTimeout = new TimeSpan(2, 0, 0);

            if (IsJobDisabled())
            {
                return;
            }
            RepeatTillTimeout(dataGenerator.Generate, dataGeneratorTimeout, backoffPolicy);
            DisableJob();
        }

        private static bool IsJobDisabled()
        {
            return File.Exists(DisableFilePath);
        }

        private static void DisableJob()
        {
            File.Create(DisableFilePath).Dispose();
        }

        private static void RepeatTillTimeout(VoidFunction voidFunction, TimeSpan timeout, IBackoffPolicy backoffPolicy)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < timeout)
            {
                try
                {
                    voidFunction();
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e);
                }
                backoffPolicy.Wait();
            }
        }

        private delegate void VoidFunction();
    }
}