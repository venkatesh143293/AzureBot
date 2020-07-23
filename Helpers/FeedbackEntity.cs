using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreBot.Helpers
{
    public class FeedbackEntity : TableEntity
    {
        public string User => PartitionKey;
        public string Role { get; set; }
        public string FeedBack { get; set; }
        public string Status { get; set; }          
        public string Intent { get; set; }
        public string Project { get; set; }
    }

    public class RoleAction : TableEntity
    {
        public string Action { get; set; }
        public string SubAction { get; set; }
    }
}
