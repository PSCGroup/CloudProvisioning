using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using OfficeDevPnP.Core.Framework.Provisioning.Providers.Xml;
using OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using Microsoft.SharePoint.Client.Publishing;
using CloudProvisioning.Common;

namespace CloudProvisioningWeb.Common
{

    public class SiteProvisioningFactory
    {

        //Contstants

        /// <summary>
        /// ListViewWebPart default XML
        /// </summary>
        private const string ListViewWebPartXml = "<webParts>" +
                             "<webPart xmlns='http://schemas.microsoft.com/WebPart/v3'>" +
                             "<metaData>" +
                             "<type name='Microsoft.SharePoint.WebPartPages.XsltListViewWebPart, Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c' />" +
                             "<importErrorMessage>Cannot import this Web Part.</importErrorMessage>" +
                             "</metaData>" +
                             "<data>" +
                             "<properties>" +
                             "<property name='ShowWithSampleData' type='bool'>False</property>" +
                             "<property name='Default' type='string' />" +
                             "<property name='NoDefaultStyle' type='string' null='true' />" +
                             "<property name='CacheXslStorage' type='bool'>True</property>" +
                             "<property name='ViewContentTypeId' type='string' />" +
                             "<property name='XmlDefinitionLink' type='string' />" +
                             "<property name='ManualRefresh' type='bool'>False</property>" +
                             "<property name='ListUrl' type='string' />" +
                             "<property name='ListId' type='System.Guid, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'>{0}</property>" +
                             "<property name='TitleUrl' type='string'></property>" +
                             "<property name='EnableOriginalValue' type='bool'>False</property>" +
                             "<property name='Direction' type='direction'>NotSet</property>" +
                             "<property name='ServerRender' type='bool'>False</property>" +
                             "<property name='ViewFlags' type='Microsoft.SharePoint.SPViewFlags, Microsoft.SharePoint, Version=16.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c'>Html, TabularView, Hidden, Mobile</property>" +
                             "<property name='AllowConnect' type='bool'>True</property>" +
                             "<property name='ListName' type='string'>{0}</property>" +
                             "<property name='ListDisplayName' type='string' />" +
                             "<property name='Title' type='string'>{1}</property>" +
                             "</properties>" +
                             "</data>" +
                             "</webPart>" +
                             "</webParts>";

        //Class variables
        public string SiteCollectionRequestsListTitle
        {
            get;
            set;
        }

        public string SubsiteRequestsListTitle
        {
            get;
            set;
        }


        public string SiteTemplatesListTitle 
        { 
            get; 
            set; 
        }

        public enum SiteType
        {
            SiteCollection,
            Subsite
        }

        

        /// <summary>
        /// Provisions sub-sites based on the request list items in the source web "Site Requests" list
        /// </summary>
        /// <param name="e"></param>
        public void ProvisionSites(ClientContext clientContext, SiteType siteType)
        {

            string listTitle = string.Empty;

            switch (siteType)
            {
                case SiteType.SiteCollection:
                    listTitle = SiteCollectionRequestsListTitle;
                    break;
                case SiteType.Subsite:
                    listTitle = SubsiteRequestsListTitle;
                    break;
            }

            try
            {

                //Get client sites first; throw exception if can't find list
                if (!clientContext.Web.ListExists(listTitle))
                    throw new Exception(string.Format("List {0} doesn't exist.", listTitle));

                List requestList = clientContext.Web.Lists.GetByTitle(listTitle);

                CamlQuery camlQuery = new CamlQuery();
                camlQuery.ViewXml = "<View><Query><Where><Eq><FieldRef Name=\"ProvisioningStatus\"/><Value Type=\"Text\">Requested</Value></Eq></Where></Query></View>";

                ListItemCollection requests = requestList.GetItems(camlQuery);
                clientContext.Load(requestList);
                clientContext.Load(requests);

                clientContext.ExecuteQueryRetry();

                if (requests.Count > 0)
                {
                    foreach (ListItem request in requests)
                    {
                        //Wrap in try-catch so we ran write an error to the list item if necessary
                        try
                        {
                            clientContext.Load(request);
                            clientContext.ExecuteQuery();

                            ProcessSiteRequest(clientContext, request, siteType);
                        }

                        //Update source web list item with error if exception occurs
                        catch (Exception ex)
                        {

                            clientContext.Load(request);
                            clientContext.ExecuteQuery();

                            request["Processed"] = DateTime.Now;
                            request["ProvisioningStatus"] = "Error";
                            request["ErrorMessage"] = String.Format("Error of type {0}: {1}", ex.GetType(), ex.Message);
                   

                            request.Update();
                            clientContext.ExecuteQueryRetry();
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No pending requests found.");
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine("Exception of type {0}: {1}", ex.GetType().ToString(), ex.Message);
            }
        }


        private static string GetNewSiteUrl(ListItem request)
        {
            string newSiteUrl = string.Empty;

            string siteTitle = request["Title"].ToString();

            string abbreviation = string.Empty;

            try
            {
                abbreviation = request["Abbreviation"].ToString();
            }
            catch
            {
                //No value for field
            }

            if (!abbreviation.Contains(" ") && !string.IsNullOrEmpty(abbreviation))
            {
                newSiteUrl = abbreviation.ReplaceInvalidUrlChars("-");
            }
            else
            {
                newSiteUrl = siteTitle.ReplaceInvalidUrlChars("-");
            }

            return newSiteUrl;
        }

        /// <summary>
        /// Processes the request list item
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="request"></param>
        private void ProcessSiteRequest(ClientContext ctx, ListItem request, SiteType siteType)
        {

            bool isSubSite = false;

            var web = ctx.Web;
            ctx.Load(web, w => w.Url);

            var title = request["Title"].ToString();
            Console.WriteLine("Found site request for new site with title '{0}'...", title);


            //Update list item to indicate that we are processing the request
            request["ProvisioningStatus"] = "Provisioning...";
            request.Update();
            ctx.ExecuteQueryRetry();
            Console.WriteLine("Updated list item with status 'Provisioning...'");


            string baseTemplate = string.Empty;
            string templateFileRelativeUrl = string.Empty;
            string templateFileName = string.Empty;
            string siteTitle = string.Empty;
            string siteUrl = string.Empty;
            string siteDescription = string.Empty;

            #region ProvisioningTemplate

            Console.WriteLine("Loading template...");

            //Throw exception request doesn't have a template
            var templateLookupValue = request["SiteTemplate"] as FieldLookupValue;
            if (templateLookupValue == null)
                throw new Exception(String.Format("Request with title {0} does not have a value for the Site Template field.", title));


            //Get provisioning template
            int id = templateLookupValue.LookupId;
            string lookupListTitle = "Site Templates";
            ListItem templateFile = GetListItemFromLookupList(ctx, id, lookupListTitle);
            string url = templateFile["FileRef"].ToString();
            templateFileName = templateFile["FileLeafRef"].ToString();
            templateFileRelativeUrl = url;
            
            baseTemplate = templateFile["BaseTemplate"].ToString();

            ProvisioningTemplate provisioningTemplate = GetProvisioningTemplate(ctx, this.SiteTemplatesListTitle, templateFileName);

            //Throw exception if can't find it
            if (provisioningTemplate == null)
                throw new Exception(string.Format("Could not find template {0}", templateFileName));

            //===========================================================
            //Un-comment to use template created from source web instead
            //===========================================================
            //template = clientContext.Web.GetProvisioningTemplate();
            #endregion

            #region ParentWeb
            string parentWebUrl = string.Empty;

            if (siteType == SiteType.Subsite)
            {
                var parentSiteLookupValue = request["ParentWeb"] as FieldLookupValue;
                if (parentSiteLookupValue == null)
                    throw new Exception(String.Format("Subsite request with title {0} does not have a value for the Client Site field.", title));


                //Get provisioning template
                int clientSiteItemId = parentSiteLookupValue.LookupId;
                ListItem parentSiteListItem = GetListItemFromLookupList(ctx, clientSiteItemId, this.SiteCollectionRequestsListTitle);
                ctx.Load(parentSiteListItem, p => p["LinkToProvisionedSite"]);

                var parentWeb = parentSiteListItem["LinkToProvisionedSite"] as FieldUrlValue;
                parentWebUrl = parentWeb.Url;

                isSubSite = true;
            }


            #endregion
            
            //Log all parameters before continuing
            siteTitle = request["Title"].ToString();
            siteUrl = GetNewSiteUrl(request);
            

            Console.WriteLine("Found site request.  Properties: {0}");
            Console.WriteLine("Base template: {0}", baseTemplate);
            Console.WriteLine("Template file: {0}", templateFileRelativeUrl);
            Console.WriteLine("Template file name: {0}", templateFileName);
            Console.WriteLine("Requested title: {0}", siteTitle);
            Console.WriteLine("Requested URL: {0}", siteUrl);

            //Execute in limited try/catch scope so new web is deleted if any exceptions are thrown
            try
            {

                string newWebUrl = string.Empty;

                if (isSubSite)
                {
                    //Additional owners are added to parent site Owners group because sub-sites inherit security
                    string additionalOwnerEmail = GetProjectLeaderEmail(ctx, request);
                    newWebUrl = CreateSubsite(ctx, parentWebUrl, siteUrl, baseTemplate, siteTitle, "", additionalOwnerEmail, provisioningTemplate);
                    
                }
                else
                {
                    newWebUrl = CreateSiteCollection(ctx, ctx.Web.Url, siteUrl, baseTemplate, siteTitle, "", provisioningTemplate);
                }

                //Update list item fields in list in source web
                request["Processed"] = DateTime.Now;
                FieldUrlValue linkToSite = new FieldUrlValue();
                linkToSite.Url = newWebUrl;
                linkToSite.Description = siteTitle;

                request["ProvisioningStatus"] = "Provisioned";
                request["LinkToProvisionedSite"] = linkToSite;
                request["ErrorMessage"] = "";

                request.Update();


                ctx.ExecuteQueryRetry();
            }

            catch (Exception ex)
            {
                Console.WriteLine("Exception of type {0} occurred creating or applying template to new web: {1}", ex.GetType(), ex.Message);

                Console.WriteLine("Stack trace: {0}", ex.StackTrace);

                Console.WriteLine("Attempting to roll back web creation...");

                if (ctx.Web.WebExists(siteUrl))
                {
                    var newWeb = ctx.Web.GetWeb(siteUrl);
                    ctx.Load(newWeb);
                    ctx.ExecuteQuery();

                    newWeb.DeleteObject();

                    ctx.ExecuteQuery();
                    Console.WriteLine("...Deleted newly-created web.");
                }

                //Bubble up
                throw;
            }
        }


        private string GetProjectLeaderEmail(ClientContext ctx, ListItem request)
        {
            ctx.Load(request, r => r["ProjectLeader"]);
            ctx.ExecuteQueryRetry();

            FieldUserValue fuv = (FieldUserValue)request["ProjectLeader"];

            return fuv.Email;
        }

        /// <summary>
        /// Retrieve a list item from a lookup list
        /// </summary>
        /// <param name="clientContext"></param>
        /// <param name="lookupItemId"></param>
        /// <param name="lookupListTitle"></param>
        /// <returns></returns>
        private static ListItem GetListItemFromLookupList(ClientContext clientContext, int lookupItemId, string lookupListTitle)
        {
            List lookupList = clientContext.Web.Lists.GetByTitle(lookupListTitle);

            //Get template from the library in the host web
            CamlQuery query = new CamlQuery();
            query.ViewXml = "<View><Query><Where><Eq><FieldRef Name=\"ID\"/><Value Type=\"Number\">" + lookupItemId + "</Value></Eq></Where></Query></View>";
            ListItemCollection items = lookupList.GetItems(query);
            clientContext.Load(lookupList);
            clientContext.Load(items);
            clientContext.ExecuteQueryRetry();

            if (items.Count == 0)
                throw new Exception(String.Format("Could not find item with ID {0} in list {1}.", lookupItemId, lookupListTitle));


            ListItem item = items[0];
            return item;
        }


     

        /// <summary>
        /// Returns a ProvisioningTemplate object based on the XML file with the provided templateName in the library with the provided libraryName
        /// </summary>
        /// <param name="clientContext">Context</param>
        /// <param name="libraryName">Name of the library containing the file</param>
        /// <param name="templateName">Name of the template file</param>
        /// <returns></returns>
        private static ProvisioningTemplate GetProvisioningTemplate(ClientContext clientContext, string libraryName, string templateName)
        {

            Web thisWeb = clientContext.Web;
            clientContext.Load(thisWeb);
            clientContext.ExecuteQuery();

            //Parameters for below XMLSharePointTemplateProvider constructor method are not documented.
            //More info below is taken from parent class method definition here: https://github.com/OfficeDev/PnP/blob/master/OfficeDevPnP.Core/OfficeDevPnP.Core/Framework/Provisioning/Connectors/SharePointConnector.cs

            //ClientContext clientContext: the client context
            //string connectionString: web URL (e.g. https://yourtenant.sharepoint.com/sites/dev)
            //string container: library + folder containing the file (e.g. Documents/MyFolder, or just Documents)
            //Get the XML file
            XMLSharePointTemplateProvider provider = new XMLSharePointTemplateProvider(clientContext, thisWeb.Url, libraryName);

            // Get the available, valid templates
            var templates = provider.GetTemplates();
            foreach (var template1 in templates)
            {
                Console.WriteLine("Found template with ID {0}", template1.Id);
            }
            //Load the template
            ProvisioningTemplate template = provider.GetTemplate(templateName);
            

            
            
            return template;
        }

        /// <summary>
        /// Creates a new web as a sub-site of the current context web.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="baseTemplate"></param>
        /// <param name="siteTitle"></param>
        /// <param name="siteUrl"></param>
        /// <param name="siteDescription"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        private static string CreateSubsite(ClientContext ctx, string parentWebFullUrl, string url, string template, string title, string description, string additionalOwnerEmail = "", ProvisioningTemplate provisioningTemplate = null)
        {
            string newWebUrl = string.Empty;

            var parentWebUri = new Uri(parentWebFullUrl);
            string realm = TokenHelper.GetRealmFromTargetUrl(parentWebUri);
            var token = TokenHelper.GetAppOnlyAccessToken(TokenHelper.SharePointPrincipal, parentWebUri.Authority, realm).AccessToken;
            
            using (var parentWebCtx = TokenHelper.GetClientContextWithAccessToken(parentWebFullUrl.ToString(), token))
            {
                var web = parentWebCtx.Web;
                parentWebCtx.Load(web, w => w.Url, w=>w.Navigation);
                parentWebCtx.ExecuteQuery();


                if (parentWebCtx != null)
                {
                    string parentWebUrlUrl = web.Url;
                    if (!parentWebCtx.WebExistsFullUrl(parentWebUrlUrl + "/" + url))
                    {

                        var newWeb = web.CreateWeb(
                            new OfficeDevPnP.Core.Entities.SiteEntity()
                            {
                                Title = title,
                                Url = url,
                                Description = description,
                                Template = template
                            }
                        );

                       
                        parentWebCtx.Load(newWeb, n => n.Url, n=>n.Title);
                        parentWebCtx.ExecuteQuery();
                        newWebUrl = newWeb.Url;

                        //Apply template
                        if(provisioningTemplate !=null)
                        {
                            //Delegate for logging
                            var applyingInfo = new ProvisioningTemplateApplyingInformation();
                            applyingInfo.ProgressDelegate = (message, step, total) =>
                            {
                                Console.WriteLine("{0}/{1} Provisioning {2}", step, total, message);
                            };

                            
                            //Apply template
                            newWeb.ApplyProvisioningTemplate(provisioningTemplate, applyingInfo);

                            //
                            ApplyPostTemplateModifications(newWeb.Url, SiteType.Subsite);

                            //Add to parent web quick launch
                            web.AddNavigationNode(newWeb.Title, new Uri(newWeb.Url), "Projects", OfficeDevPnP.Core.Enums.NavigationType.QuickLaunch);

                            //Add additional owners to parent web
                            if (!String.IsNullOrEmpty(additionalOwnerEmail))
                            {
                                parentWebCtx.Load(web, w => w.SiteGroups);
                                parentWebCtx.ExecuteQuery();


                                var group = web.SiteGroups.GetByName("PSC Owners");
                                var user = web.EnsureUser(additionalOwnerEmail);
                                group.Users.AddUser(user);
                                web.Update();
                                parentWebCtx.ExecuteQueryRetry();
                            }

                        }
                        
                    }

                }
                else
                {
                    throw new Exception(String.Format("The parent web at URL {0} doesn't exist.", parentWebFullUrl));
                }
            }
            return newWebUrl;


        }

        private static void UploadAssets(ClientContext ctx, Web web, bool rethrow = false)
        {
            Console.WriteLine("Begin file upload...");
            try
            {
                // Get the path to the file which we are about to deploy
                //string file = System.Web.Hosting.HostingEnvironment.MapPath(string.Format("~/{0}", "Content/MyPSC.png"));
                Console.WriteLine("Environment web root path: {0}", Environment.GetEnvironmentVariable("WEBROOT_PATH"));
                string file = Environment.GetEnvironmentVariable("WEBROOT_PATH") + "\\Content\\MyPSC.png";
                Console.WriteLine("Attempting to upload file {0}...", file);
                //Get site assets library
                List assetLibrary = web.GetListByTitle("Site Assets");
                ctx.Load(assetLibrary, a => a.RootFolder);

                // Use CSOM to uplaod the file in
                FileCreationInformation newFile = new FileCreationInformation();
                newFile.Content = System.IO.File.ReadAllBytes(file);
                newFile.Url = "MyPSC.png";
                newFile.Overwrite = true;
                Microsoft.SharePoint.Client.File uploadFile = assetLibrary.RootFolder.Files.Add(newFile);
                ctx.Load(uploadFile);
                ctx.ExecuteQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error uploading file: {0}" + ex.Message);
                if (rethrow)
                {
                    throw;
                }
            }

        }

        private static void ApplyPostTemplateModifications(string webFullUrl, SiteType siteType)
        {
            
            var webUri = new Uri(webFullUrl);
            string realm = TokenHelper.GetRealmFromTargetUrl(webUri);
            var token = TokenHelper.GetAppOnlyAccessToken(TokenHelper.SharePointPrincipal, webUri.Authority, realm).AccessToken;

            using (var ctx = TokenHelper.GetClientContextWithAccessToken(webFullUrl.ToString(), token))
            {

                var web = ctx.Web; 
                ctx.Load(web, w => w.Url, w => w.Title);


                //If Site Collection (parent)
                if (siteType == SiteType.SiteCollection)
                {
                    UpdateQuickLaunch(ctx, web, SiteType.SiteCollection);
                    UpdateSecurity(ctx, web);
                    UpdateHomePage(ctx, web, SiteType.SiteCollection);
                    UploadAssets(ctx, web);
                }

                //Subsite (child)
                else
                {
                    UpdateQuickLaunch(ctx, web, SiteType.Subsite);
                    UpdateHomePage(ctx, web, SiteType.Subsite);
                    
                }
            }
            
        }

        private static void UpdateHomePage(ClientContext ctx, Web web, SiteType siteType)
        {
            ctx.Load(web, w => w.Url);
            ctx.ExecuteQueryRetry();
            string pageUrl = "Home.aspx";

            //Update page layout
            web.AddLayoutToWikiPage("SitePages", OfficeDevPnP.Core.WikiPageLayout.OneColumn, pageUrl);

            //Site collection (parent)
            if (siteType == SiteType.SiteCollection)
            {
                AddListViewWebPartToPage(ctx, web, "Contacts", pageUrl, 1, 1);
                AddListViewWebPartToPage(ctx, web, "Shared Documents", pageUrl, 1, 1);
            }

            //Subsite (child)
            else
            {

                AddListViewWebPartToPage(ctx, web, "Shared Documents", pageUrl, 1, 1);
                AddListViewWebPartToPage(ctx, web, "RAID Log", pageUrl, 1, 1);
                AddListViewWebPartToPage(ctx, web, "Contacts", pageUrl, 1, 1);
                AddListViewWebPartToPage(ctx, web, "Calendar", pageUrl, 1, 1);
            }
        }

        private static void AddListViewWebPartToPage(ClientContext ctx, Web web, string listTitle, string pageName, int row, int col)
        {
            List list = web.GetListByTitle(listTitle);
            ctx.Load(list, l => l.Id, l => l.Title);
            ctx.ExecuteQueryRetry();

            OfficeDevPnP.Core.Entities.WebPartEntity wp = new OfficeDevPnP.Core.Entities.WebPartEntity
            {
                WebPartIndex = 2,
                WebPartTitle = list.Title,
                WebPartZone = "Left", //Not sure if we need this
                WebPartXml = String.Format(ListViewWebPartXml, list.Id, list.Title)
            };



            web.AddWebPartToWikiPage("SitePages", wp, pageName, row, col, true);

        }


        private static void UpdateSecurity(ClientContext ctx, Web web)
        {

            Group pscOwners = web.SiteGroups.GetByName("PSC Owners");
            Group pscMembers = web.SiteGroups.GetByName("PSC Members");
            Group pscVisitors = web.SiteGroups.GetByName("PSC Visitors");

            web.AssociateDefaultGroups(pscOwners, pscMembers, pscVisitors);
            web.Update();
            ctx.ExecuteQueryRetry();

            Group defaultOwners = web.SiteGroups.GetByName(web.Title + " Owners");
            Group defaultMembers = web.SiteGroups.GetByName(web.Title + " Members");
            Group defaultVisitors = web.SiteGroups.GetByName(web.Title + " Visitors");


            if (defaultOwners != null)
                web.SiteGroups.Remove(defaultOwners);
            if (defaultMembers != null)
                web.SiteGroups.Remove(defaultMembers);
            if (defaultVisitors != null)
                web.SiteGroups.Remove(defaultVisitors);

            try
            {
                web.Update();
                ctx.ExecuteQueryRetry();
            }
            catch (Exception)
            {
                //It's OK-- group doesn't exist
            }
        }


        private static string GetSiteNotebookUrl(ClientContext ctx, Web web)
        {
            ctx.Load(web, w => w.Url, w=>w.Title);
            ctx.ExecuteQueryRetry();
            return web.Url + "/SiteAssets/" + web.Title + " Notebook";
        }

        private static string GetSiteContentsUrl(ClientContext ctx, Web web)
        {
            ctx.Load(web, w => w.Url);
            ctx.ExecuteQueryRetry();
            return web.Url + "/_layouts/15/viewlsts.aspx";
        }

        private static void UpdateQuickLaunch(ClientContext ctx, Web web, SiteType siteType)
        {

            ctx.Load(web, w => w.ParentWeb);


            //Site Collection (parent)
            if (siteType == SiteType.SiteCollection)
            {
                web.DeleteAllNavigationNodes(OfficeDevPnP.Core.Enums.NavigationType.QuickLaunch);

                //Quick launch
                web.AddNavigationNode(web.Title, new Uri(web.Url), "", OfficeDevPnP.Core.Enums.NavigationType.QuickLaunch);
                web.AddNavigationNode("Projects", new Uri(web.Url), "", OfficeDevPnP.Core.Enums.NavigationType.QuickLaunch);
                AddListToQuickLaunch(ctx, web, "Shared Documents", web.Title);
                AddListToQuickLaunch(ctx, web, "Contacts", web.Title);
                web.AddNavigationNode("Notebook", new Uri(GetSiteNotebookUrl(ctx, web)), web.Title, OfficeDevPnP.Core.Enums.NavigationType.QuickLaunch);
                web.AddNavigationNode("Site Contents", new Uri(GetSiteContentsUrl(ctx, web)), web.Title, OfficeDevPnP.Core.Enums.NavigationType.QuickLaunch);
            }

            //Subsite (child)
            else
            {
                //Navigation settings
                NavigationExtensions.UpdateNavigationInheritance(web, true);

                web.DeleteAllNavigationNodes(OfficeDevPnP.Core.Enums.NavigationType.QuickLaunch);

                //Quick launch
                web.AddNavigationNode(web.Title, new Uri(web.Url), "", OfficeDevPnP.Core.Enums.NavigationType.QuickLaunch);
                AddListToQuickLaunch(ctx, web, "Shared Documents", web.Title);
                AddListToQuickLaunch(ctx, web, "RAID Log", web.Title);
                AddListToQuickLaunch(ctx, web, "Calendar", web.Title);
                AddListToQuickLaunch(ctx, web, "Contacts", web.Title);
                web.AddNavigationNode("Notebook", new Uri(GetSiteNotebookUrl(ctx, web)), web.Title, OfficeDevPnP.Core.Enums.NavigationType.QuickLaunch);
                web.AddNavigationNode("Site Contents", new Uri(GetSiteContentsUrl(ctx, web)), web.Title, OfficeDevPnP.Core.Enums.NavigationType.QuickLaunch);

            }


        }


        private static void AddListToQuickLaunch(ClientContext ctx, Web web, string listTitle, string parentNodeTitle = "")
        {
            if (web.ListExists(listTitle))
            {
                List list = web.GetListByTitle(listTitle);

                ctx.Load(list, s => s.DefaultViewUrl, s => s.Title);
                ctx.ExecuteQuery();
                web.AddNavigationNode(listTitle, new Uri(list.DefaultViewUrl, UriKind.Relative), parentNodeTitle, OfficeDevPnP.Core.Enums.NavigationType.QuickLaunch);

            }
        }


        /// <summary>
        /// Creates a site collection
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="hostWebUrl"></param>
        /// <param name="url"></param>
        /// <param name="template"></param>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        private static string CreateSiteCollection(ClientContext ctx, string hostWebUrl, string url, string template, string title, string description, ProvisioningTemplate provisioningTemplate = null)
        {
            //get the base tenant admin urls
            var tenantStr = hostWebUrl.ToLower().Replace("-my", "").Substring(8);
            tenantStr = tenantStr.Substring(0, tenantStr.IndexOf("."));

            //get the current user to set as owner
            var currUser = ctx.Web.CurrentUser;
            
            ctx.Load(currUser);
            ctx.ExecuteQuery();

            //create site collection using the Tenant object
            var webUrl = String.Format("https://{0}.sharepoint.com/{1}/{2}", tenantStr, "sites", url);
            var tenantAdminUri = new Uri(String.Format("https://{0}-admin.sharepoint.com", tenantStr));
            string realm = TokenHelper.GetRealmFromTargetUrl(tenantAdminUri);
            var token = TokenHelper.GetAppOnlyAccessToken(TokenHelper.SharePointPrincipal, tenantAdminUri.Authority, realm).AccessToken;
            using (var adminContext = TokenHelper.GetClientContextWithAccessToken(tenantAdminUri.ToString(), token))
            {
                var tenant = new Tenant(adminContext);
                var properties = new SiteCreationProperties()
                {
                    Url = webUrl,
                    Owner = "cnjohnson@psclistens.com", // currUser.Email,
                    Title = title,
                    Template = template,
                    StorageMaximumLevel = 100,
                    UserCodeMaximumLevel = 100
                };

                //start the SPO operation to create the site
                if (tenant.SiteExists(webUrl))
                    throw new Exception(String.Format("A site at URL {0} already exists.", webUrl));
                SpoOperation op = tenant.CreateSite(properties);
                adminContext.Load(tenant);
                adminContext.Load(op, i => i.IsComplete);
                adminContext.ExecuteQuery();

                // Set timeout for the request - notice that since we are using web site, this could still time out
                adminContext.RequestTimeout = Timeout.Infinite;

                //check if site creation operation is complete
                while (!op.IsComplete)
                {
                    //wait 30seconds and try again
                    System.Threading.Thread.Sleep(30000);
                    op.RefreshLoad();
                    adminContext.ExecuteQuery();
                }
            }

            //get the new site collection
            var siteUri = new Uri(webUrl);
            token = TokenHelper.GetAppOnlyAccessToken(TokenHelper.SharePointPrincipal, siteUri.Authority, realm).AccessToken;
            using (var newWebContext = TokenHelper.GetClientContextWithAccessToken(siteUri.ToString(), token))
            {
                var newWeb = newWebContext.Web;
                newWebContext.Load(newWeb);
                newWebContext.ExecuteQuery();

                if (provisioningTemplate != null)
                {
                    //Delegate for logging
                    var applyingInfo = new ProvisioningTemplateApplyingInformation();
                    applyingInfo.ProgressDelegate = (message, step, total) =>
                    {
                        Console.WriteLine("{0}/{1} Provisioning {2}", step, total, message);
                    };

                    newWeb.ApplyProvisioningTemplate(provisioningTemplate, applyingInfo);

                    //Post-template changes
                    ApplyPostTemplateModifications(newWeb.Url, SiteType.SiteCollection);
                }

                // All done, let's return the newly created site
                return newWeb.Url;
            }

        }
    }
}