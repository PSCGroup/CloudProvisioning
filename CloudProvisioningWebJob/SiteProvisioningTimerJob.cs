using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Framework.TimerJobs;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using OfficeDevPnP.Core.Framework.Provisioning.Providers.Xml;
using OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers;
using CloudProvisioningWeb.Common;


namespace CloudProvisioningWebJob
{
    class SiteProvisioningTimerJob : TimerJob
    {

        public SiteProvisioningTimerJob()
            : base("SiteProvisioningTimerJob")
        {
            TimerJobRun += SiteProvisioningTimerJob_TimerJobRun;
        }

        /// <summary>
        /// Timer job run definition
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SiteProvisioningTimerJob_TimerJobRun(object sender, TimerJobRunEventArgs e)
        {

            try
            {
                e.WebClientContext.Load(e.WebClientContext.Web, p => p.Title);
                e.WebClientContext.ExecuteQueryRetry();
                var ctx = e.WebClientContext;
                Console.WriteLine("Opened site {0} with title {1}", e.Url, e.WebClientContext.Web.Title);

                SiteProvisioningFactory spf = new SiteProvisioningFactory
                {
                    SiteCollectionRequestsListTitle = "Client Sites"
                    ,
                    SubsiteRequestsListTitle = "Project Sites"
                    ,
                    SiteTemplatesListTitle = "Site Templates"
                };

                //Provision sites
                spf.ProvisionSites(ctx, SiteProvisioningFactory.SiteType.SiteCollection);

                //Provision subsites
                spf.ProvisionSites(ctx, SiteProvisioningFactory.SiteType.Subsite);
            }

            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex.Message);
            }


        }


    }
}
