﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ClusterClient.Models
{
    public enum ServerStatusCode
    {
        /// <summary>
        /// Status code has not been set specifically.
        /// </summary>
        None = 0,

        /// <summary>
        /// The server has a response.
        /// </summary>
        OK = 1,

        /// <summary>
        /// Some input has been classified as nonsense.
        /// </summary>
        Nonsense = 2,

        /// <summary>
        /// Some input has been classified as offensive.
        /// </summary>
        Offensive = 3
    }
    
    public enum FeedbackStatusCode
    {
        /// <summary>
        /// The user provided negative feedback.
        /// </summary>
        Negative = 0,

        /// <summary>
        /// The user provided positive feedback.
        /// </summary>
        Positive = 1
    }
}
