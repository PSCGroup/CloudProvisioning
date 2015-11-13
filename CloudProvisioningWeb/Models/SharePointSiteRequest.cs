using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.SharePoint.Client;

namespace CloudProvisioningWeb.Models
{
    public class SiteTemplate
    {

        public string Title { get; set; }

        public string BaseTemplate { get; set; }

        public string Description { get; set; }

        public string TemplateUrl { get; set; }
    }


    public class SharePointSiteRequest
    {
        #region Properties
        public string Title { get; set; }

        public string Url { get; set; }

        public string Description { get; set; }

        public int Id { get; set; }

        public DateTime Requested { get; set; }

        public String RequestedByEmail { get; set; }

        public String OwnerDisplayName { get; set; }

        public String OwnerEmail { get; set; }

        public DateTime ProcessedOn { get; set; }

        public string ErrorMessage { get; set; }

        public SiteTemplate SiteTemplate { get; set; }

        public string LinkToProvisionedSite { get; set; }

        public string ProvisioningStatus { get; set; }

        #endregion

    }
}
