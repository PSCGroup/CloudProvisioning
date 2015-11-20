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

                    string siteCollectionsListTitle = "Client Site Collections";
                    string subsitesListTitle = "Project Subsites";
                    string siteTemplatesListTitle = "Site Templates";

                    spUser = ctx.Web.CurrentUser;
                    var spWeb = ctx.Web;
                    ctx.Load(spUser, user => user.Title);
                    ctx.Load(spWeb, web => web.Url, web => web.ServerRelativeUrl);
                    ctx.ExecuteQuery();

                    bool listsExist = AppInstallationHelper.RequiredListsExist(ctx);
                    List siteCollectionsList = ctx.Web.Lists.GetByTitle(siteCollectionsListTitle);
                    List subsitesList = ctx.Web.Lists.GetByTitle(subsitesListTitle);
                    List siteTemplatesList = ctx.Web.Lists.GetByTitle(siteTemplatesListTitle);
                    ctx.Load(siteCollectionsList, list => list.DefaultViewUrl, list=>list.Description);
                    ctx.Load(subsitesList, list => list.DefaultViewUrl, list => list.Description);
                    ctx.Load(siteTemplatesList, list => list.DefaultViewUrl, list => list.Description); 
                    ctx.ExecuteQuery();

                    string serverRelativeUrl = spWeb.ServerRelativeUrl;

                    string siteCollectionListUrl = spWeb.Url +  siteCollectionsList.DefaultViewUrl.Replace(serverRelativeUrl, "");
                    string subsiteListUrl = spWeb.Url +  subsitesList.DefaultViewUrl.Replace(serverRelativeUrl, "");
                    string siteTemplatesListUrl = spWeb.Url + siteTemplatesList.DefaultViewUrl.Replace(serverRelativeUrl, "");


                    ViewBag.SiteCollectionsListUrl = siteCollectionListUrl;
                    ViewBag.SubsitesListUrl = subsiteListUrl;
                    ViewBag.SiteTemplatesListUrl = siteTemplatesListUrl;

                    ViewBag.SiteCollectionsListTitle = siteCollectionsListTitle;
                    ViewBag.SubsitesListTitle = subsitesListTitle;
                    ViewBag.SiteTemplatesListTitle = siteTemplatesListTitle;

                    ViewBag.SiteCollectionsListDescription = siteCollectionsList.Description;
                    ViewBag.SubsitesListDescription = subsitesList.Description;
                    ViewBag.SiteTemplatesListDescription = siteTemplatesList.Description;

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
