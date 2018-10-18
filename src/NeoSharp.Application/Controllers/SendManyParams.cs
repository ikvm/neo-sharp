// <copyright file="PromptRpcController.cs" company="City of Zion">
// Copyright (c) 2018 All Rights Reserved
// </copyright>

using System.Numerics;
using NeoSharp.Types;
using Newtonsoft.Json;

namespace NeoSharp.Application.Controllers
{
    public class SendManyParams
    {
        [JsonProperty("asset")]
        public UInt256 Asset { get; set; }

        [JsonProperty("address")]
        public UInt160 Address { get; set; }

        [JsonProperty("value")]
        public BigInteger Value { get; set; }
    }
}