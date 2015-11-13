using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CloudProvisioningWeb.Models;

namespace CloudProvisioningWeb.Controllers
{
    public class RequestController : Controller
    {

        [SharePointContextFilter]
        public ActionResult New()
        {
            User spUser = null;

            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext);

            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                if (clientContext != null)
                {
                    spUser = clientContext.Web.CurrentUser;

                    clientContext.Load(spUser, user => user.Title);

                    clientContext.ExecuteQuery();

                    ViewBag.UserName = spUser.Title;
                }
            }

            return View();
        }

        [SharePointContextFilter]
        public ActionResult View(int id)
        {

            SharePointSiteRequest siteRequest = GetSiteRequest(id);

            return View(siteRequest);
            
        }

        [SharePointContextFilter]
        public ActionResult Edit(int id)
        {

            SharePointSiteRequest siteRequest = GetSiteRequest(id);

            return View(siteRequest);

        }

      
        [SharePointContextFilter]
        public ActionResult Requests()
        {
            List<SharePointSiteRequest> siteRequests = new List<SharePointSiteRequest>();

            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext);

            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                if (clientContext != null)
                {
                    List oList = clientContext.Web.Lists.GetByTitle("Site Requests");
                    CamlQuery camlQuery = CamlQuery.CreateAllItemsQuery();

                    ListItemCollection requests = oList.GetItems(camlQuery);

                    clientContext.Load(requests);
                    clientContext.ExecuteQuery();

                    foreach (ListItem request in requests)
                    {

                        
                        //Required fields-- will have a value
                        int requestId = request.Id;
                        string title = request["Title"].ToString();
                        string url = request["SiteUrl"].ToString();
                        string description = request["SiteDescription"].ToString();

                        //Strongly-typed Site Template
                        var siteTemplate = GetSiteTemplateFromRequest(clientContext, request["SiteTemplate"] as FieldLookupValue);
                        
                        //Owner: Person or Group
                        var owner = (FieldUserValue)request["SiteOwner"];
                        string ownerEmail = owner.Email;

                        //Created By: Person or Group
                        var createdBy = (FieldUserValue)request["Author"];
                        string requestedByEmail = createdBy.Email;
                        
                        //Requested: DateTime
                        DateTime requested = DateTime.Parse(request["Created"].ToString());
                        
                        //Optional or read-only fields-- may not have a value so need to try/catch
                        
                        //Processed: DateTime field, but only parse to DateTime if not empty
                        string processed;
                        try
                        {
                            processed = request["ProcessedOn"].ToString();
                        }
                        catch (NullReferenceException)
                        {
                            processed = null;
                        }

                        string errorMessage;
                        try
                        {
                            errorMessage = request["ErrorMessage"].ToString();
                        }
                        catch(NullReferenceException)
                        {
                            errorMessage = string.Empty;
                        }


                        string provisioningStatus;
                        try
                        {
                            provisioningStatus = request["ProvisioningStatus"].ToString();
                        }
                        catch (NullReferenceException)
                        {
                            provisioningStatus = string.Empty;
                        }
                        

                        var linkToSite = GetLinkToSiteUrl(request["LinkToProvisionedSite"] as FieldUrlValue);
                        string linkToProvisionedSite = linkToSite;
 

                        SharePointSiteRequest existingRequest = new SharePointSiteRequest
                        {
                            Id = requestId 
                            , OwnerDisplayName = owner.ToString()
                            , Title = title
                            , Url = url
                            , Description = description
                            , OwnerEmail = ownerEmail
                            , ErrorMessage = errorMessage
                            , Requested = requested
                            , LinkToProvisionedSite = linkToProvisionedSite
                            , ProvisioningStatus = provisioningStatus
                            , RequestedByEmail = requestedByEmail
                            , SiteTemplate = siteTemplate

                        };

                        if (!String.IsNullOrEmpty(processed))
                        {
                            existingRequest.ProcessedOn = DateTime.Parse(processed);
                        }

                        siteRequests.Add(existingRequest);
                    }


                }
            }

            return View(siteRequests);
        }

        #region Helpers

        /// <summary>
        /// Get the site request with the specified Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private SharePointSiteRequest GetSiteRequest(int id)
        {
            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext);
            SharePointSiteRequest siteRequest = null;
            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {


                if (clientContext != null)
                {
                    List oList = clientContext.Web.Lists.GetByTitle("Site Requests");

                    ListItem request = oList.GetItemById(id);


                    clientContext.Load(request);
                    clientContext.ExecuteQuery();

                    if (request != null)
                    {


                        //Required fields-- will have a value
                        int requestId = request.Id;
                        string title = request["Title"].ToString();
                        string url = request["SiteUrl"].ToString();
                        string description = request["SiteDescription"].ToString();

                        //Strongly-typed Site Template
                        var siteTemplate = GetSiteTemplateFromRequest(clientContext, request["SiteTemplate"] as FieldLookupValue);

                        //Owner: Person or Group
                        var owner = (FieldUserValue)request["SiteOwner"];
                        string ownerEmail = owner.Email;

                        //Created By: Person or Group
                        var createdBy = (FieldUserValue)request["Author"];
                        string requestedByEmail = createdBy.Email;

                        //Requested: DateTime
                        DateTime requested = DateTime.Parse(request["Created"].ToString());


                        //Processed: DateTime field, but only parse to DateTime if not empty
                        string processed;
                        try
                        {
                            processed = request["ProcessedOn"].ToString();
                        }
                        catch (NullReferenceException)
                        {
                            processed = string.Empty;
                        }

                        string errorMessage;
                        try
                        {
                            errorMessage = request["ErrorMessage"].ToString();
                        }
                        catch (NullReferenceException)
                        {
                            errorMessage = string.Empty;
                        }


                        string provisioningStatus;
                        try
                        {
                            provisioningStatus = request["ProvisioningStatus"].ToString();
                        }
                        catch (NullReferenceException)
                        {
                            provisioningStatus = string.Empty;
                        }


                        var linkToSite = GetLinkToSiteUrl(request["LinkToProvisionedSite"] as FieldUrlValue);
                        string linkToProvisionedSite = linkToSite;


                        siteRequest = new SharePointSiteRequest
                        {
                            Id = requestId
                            ,
                            Title = title
                            ,
                            Url = url
                            ,
                            Description = description
                            ,
                            OwnerEmail = ownerEmail
                            ,
                            ErrorMessage = errorMessage
                            ,
                            Requested = requested
                            ,
                            LinkToProvisionedSite = linkToProvisionedSite
                            ,
                            ProvisioningStatus = provisioningStatus
                            ,
                            RequestedByEmail = requestedByEmail

                        };

                        if (!String.IsNullOrEmpty(processed))
                        {
                            siteRequest.ProcessedOn = DateTime.Parse(processed);
                        }



                    }


                }
                return siteRequest;
            }
        }

        /// <summary>
        /// Get the string representation of the link to the provisioned site
        /// </summary>
        /// <param name="linkValue"></param>
        /// <returns></returns>
        private string GetLinkToSiteUrl(FieldUrlValue linkValue){
            if (linkValue != null)
                return linkValue.Url;
            else
                return string.Empty;
        }

        /// <summary>
        /// Get the site template from the Templates library based on the lookup value
        /// </summary>
        /// <param name="clientContext"></param>
        /// <param name="templateLookupValue"></param>
        /// <returns></returns>
        private SiteTemplate GetSiteTemplateFromRequest(ClientContext clientContext, FieldLookupValue templateLookupValue)
        {

            if (templateLookupValue != null)
            {

                //Get lookup list
                List templateLibrary = clientContext.Web.Lists.GetByTitle("Site Templates");


                //Get template from the library in the host web
                CamlQuery templateCamlQuery = new CamlQuery();
                templateCamlQuery.ViewXml = "<View><Query><Where><Eq><FieldRef Name=\"ID\"/><Value Type=\"Number\">" + templateLookupValue.LookupId + "</Value></Eq></Where></Query></View>";
                ListItemCollection templates = templateLibrary.GetItems(templateCamlQuery);
                clientContext.Load(templateLibrary);
                clientContext.Load(templates);
                clientContext.ExecuteQueryRetry();

                if (templates.Count == 0)
                    return null;


                ListItem templateFile = templates[0];
                string url = templateFile["FileRef"].ToString();
                string title = templateFile["Title"].ToString();
                string description = templateFile["SiteDescription"].ToString();
                string baseTemplate = templateFile["BaseTemplate"].ToString();

                return new SiteTemplate
                {
                    Title = title,
                    TemplateUrl = url,
                    Description = description,
                    BaseTemplate = baseTemplate
                };
            }

            else
                return null;
        }

        #endregion

    }
}