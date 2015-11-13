using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CloudProvisioningWeb.Common;

namespace CloudProvisioningWeb.Controllers
{
    public class HomeController : Controller
    {
        [SharePointContextFilter]
        public ActionResult Index()
        {
            User spUser = null;

            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext);

            
            using (var ctx = spContext.CreateUserClientContextForSPHost())
            {
                if (ctx != null)
                {
                    spUser = ctx.Web.CurrentUser;
                    var spWeb = ctx.Web;
                    ctx.Load(spUser, user => user.Title);
                    ctx.Load(spWeb, web => web.Url, web => web.ServerRelativeUrl);
                    ctx.ExecuteQuery();

                    bool listsExist = AppInstallationHelper.RequiredListsExist(ctx);
                    List clientSitesList = ctx.Web.Lists.GetByTitle("Client Sites");
                    List projectSitesList = ctx.Web.Lists.GetByTitle("Project Sites");
                    List siteTemplatesList = ctx.Web.Lists.GetByTitle("Site Templates");
                    ctx.Load(clientSitesList, list => list.DefaultViewUrl);
                    ctx.Load(projectSitesList, list => list.DefaultViewUrl);
                    ctx.Load(siteTemplatesList, list => list.DefaultViewUrl);
                    ctx.ExecuteQuery();

                    string serverRelativeUrl = spWeb.ServerRelativeUrl;

                    string clientSitesUrl = spWeb.Url +  clientSitesList.DefaultViewUrl.Replace(serverRelativeUrl, "");
                    string projectSitesUrl = spWeb.Url +  projectSitesList.DefaultViewUrl.Replace(serverRelativeUrl, "");
                    string siteTemplatesUrl = spWeb.Url + siteTemplatesList.DefaultViewUrl.Replace(serverRelativeUrl, "");

                    ViewBag.ClientSitesUrl = clientSitesUrl;
                    ViewBag.ProjectSitesUrl = projectSitesUrl;
                    ViewBag.SiteTemplatesUrl = siteTemplatesUrl;

                    ViewBag.ListsExist = listsExist;
                    ViewBag.UserName = spUser.Title;
                }
            }

            return View();
        }

        public ActionResult Provision()
        {
            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext);


            using (var ctx = spContext.CreateUserClientContextForSPHost())
            {
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

            return View("Index");
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

    }
}
