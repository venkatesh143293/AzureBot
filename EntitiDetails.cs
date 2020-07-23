// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.BotBuilderSamples
{
    public class EntitiDetails
    {
        public string Intent { get; set; }
        public double Score { get; set; }
        public string Acronym { get; set; }
        public string TravelDate { get; set; }
        public string Project { get; set; }
        public string Tag { get; set; }
        public string Email { get; set; }
        public string Returnmsg { get; set; }
        public string Buildwar { get; set; }
        public string Portfolio { get; set; }
        public string DbInstance { get; set; }
        public string MuvKey { get; set; }
        public string Environment { get; set; }
        public string ScheduledOption { get; set; }
        public string JenkinsDelay { get; set; }
        public string HostName { get; set; }
        public string Client { get; set; }
        public string Repo { get; set; }
        public string Buildversion { get; set; }
        public string File { get; set; }
        public bool isDBRestricted { get; set; }
        public bool codeCutOff { get; set; }      
        public ManageRule manageRule { get; set; }
        public bool isForceDeployment { get; set; }
        public string DBDeploymenttype { get; set; }
        public string ScriptName { get; set; }
        public string Role { get; set; }
    }

    public class ManageRule
    {
        public string Action { get; set; }
        public string LotusNotesUser { get; set; }
        public string PresentationId { get; set; }
        public string CDMUrl { get; set; }
        public string Category { get; set; }
        public string CategoryName { get; set; }
        public string RequestDate { get; set; }
        public string DCD { get; set; }
        public string Payers { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }

        public string MidruleVersion { get; set; }

    }

    public class AcronymDetails
    {
        public string AcronymExp { get; set; }

        //test for commit
        public string Definition { get; set; }
    }
}
