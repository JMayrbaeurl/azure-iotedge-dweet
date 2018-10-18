// Copyright (c) Microsoft. All rights reserved.

namespace TemperatureSimulatorModule
{
    using System;
    using Newtonsoft.Json;

    public class MessageBody
    {
        [JsonProperty(PropertyName = "temperature")]
        public double Temperature { get; set; }

        [JsonProperty(PropertyName = "humidity")]
        public double Humidity { get; set; }

        [JsonProperty(PropertyName = "timeCreated")]
        public DateTime TimeCreated { get; set; }
    }
}
