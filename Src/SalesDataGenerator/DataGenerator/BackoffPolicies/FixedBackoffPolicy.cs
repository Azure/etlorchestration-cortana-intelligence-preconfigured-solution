// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FixedBackoffPolicy.cs" company="Microsoft">
//   Copyright (c) Microsoft.  All rights reserved.
// </copyright>
// <summary>
//   Backoff policy with fixed interval waits.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DataGenerator.BackoffPolicies
{
    using System.Threading;

    /// <summary>
    ///     Backoff policy with fixed interval waits.
    /// </summary>
    internal class FixedBackoffPolicy : IBackoffPolicy
    {
        /// <summary>Interval wait period in milliseconds.</summary>
        private readonly int _millisecondTimeout;

        /// <summary>
        ///     Initializes an instance of FixedBackoffPolicy.
        /// </summary>
        /// <param name="millisecondTimeout">Interval wait period in milliseconds.</param>
        public FixedBackoffPolicy(int millisecondTimeout)
        {
            _millisecondTimeout = millisecondTimeout;
        }

        /// <summary>
        ///     Await a fixed time interval.
        /// </summary>
        public void Wait()
        {
            Thread.Sleep(_millisecondTimeout);
        }
    }
}