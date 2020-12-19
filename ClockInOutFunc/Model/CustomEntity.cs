using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClockInOutFunc.Model
{
    public class CustomEntity : TableEntity
    {
        public string Text { get; set; }
        public string LastHeatbeatTime { get; set; }

    }
}
