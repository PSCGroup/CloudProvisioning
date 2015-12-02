using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using System.Xml.Linq;

namespace CloudProvisioningWeb.Common
{
    public class AppInstallationHelper
    {

        #region Private class constants
        /// <summary>
        /// Name used for the custom ribbon action installed for the Site Collections request list.
        /// </summary>
        private const string SiteCollRibbonActionName = "Provisioning_SiteCollectionRibbon";
        
        /// <summary>
        /// Name used for the custom ribbon action installed for the Subsites request list.
        /// </summary>
        private const string SubsiteRibbonActionName = "Provisioning_SubsiteRibbon";

        /// <summary>
        /// Name used for the custom ScriptLink action that deploys a JavaScript script reference for all pages on this web
        /// Note: this can only be scoped to a web and CANNOT be scoped to a content type, unlike the ribbon actions above
        /// </summary>
        private const string JsLinkActionName = "Provisioning_SiteCollectionLink";

        /// <summary>
        /// 
        /// </summary>
        private const string JsJQueryActionName = "Provisioning_JQueryLink";

        #endregion

        #region Private class variables

        /// <summary>
        /// List fields for site provisioning lists, used by Site Collection and Subsites requests lists
        /// If desired, modify display names and descriptions to fit a specific use case
        /// </summary>
        private static class SharedListFields
        {

            public static string Abbreviation = "<Field Name=\"Abbreviation\" DisplayName = \"Abbreviation\" Description=\"The abbreviation for the client or project name, to be used for the URL of the new site; this is the part after the slash (/).  Should not contain spaces.  If a value for this is not provided, the client or project name will be used with hyphens (-) replacing spaces.\""
                + " Type=\"Text\" ID=\"{3DE6EEF4-3A58-4CEF-A3CA-91ACE3AC27CE}\" Required=\"FALSE\"/>";

            public static string Processed = "<Field Name=\"Processed\" DisplayName = \"Processed by Provisioning Job\" Description=\"The date and time this site was processed by the provisioning job.\""
                + " Type=\"DateTime\" ID=\"{15DE41EC-75A5-449A-9124-A30F15B3915D}\" Required=\"FALSE\" ShowInEditForm=\"FALSE\" ShowInNewForm=\"FALSE\"/>";

            public static string ErrorMessage = "<Field Name=\"ErrorMessage\" DisplayName = \"Error\" Description=\"If an error occurs during site provisioning, it will be logged here.\""
                + " Type=\"Text\" ID=\"{0AF089A2-BBFB-4912-BDA4-1FAF8A00E70C}\" Required=\"FALSE\" ShowInEditForm=\"FALSE\" ShowInNewForm=\"FALSE\"/>";

            public static string SiteTemplate = "<Field Name=\"SiteTemplate\" DisplayName = \"Site Template\" Description=\"The site template to use for this site.\""
                + " Type=\"Lookup\" List=\"" + SiteTemplateListTitle + "\" ShowField=\"Title\" ID=\"{3DE91360-8235-4F5B-BBE2-59F592FC5B35}\" Required=\"TRUE\"/>";

            public static string LinkToProvisionedSite = "<Field Name=\"LinkToProvisionedSite\" DisplayName=\"Link to Site\" Description=\"Click here to open the site.\""
                + " Type=\"URL\" Format=\"Hyperlink\" ID=\"{210C37AC-A2BC-4463-9862-B54D443FB4AF}\" ShowInEditForm=\"FALSE\" ShowInNewForm=\"FALSE\"/>";

            public static string ProvisioningStatus = "<Field Name=\"ProvisioningStatus\" DisplayName=\"Provisioning Status\" Description=\"This status is updated by the provisioning engine once a request is submitted.\""
                + " Type=\"Choice\" ID=\"{E24E549F-44A6-403A-B86A-0EFD88719611}\" ShowInEditForm=\"FALSE\" ShowInNewForm=\"FALSE\"><CHOICES>"
                    + "<CHOICE>New (not requested)</CHOICE>"
                    + "<CHOICE>Requested</CHOICE>"
                    + "<CHOICE>Provisioning...</CHOICE>"
                    + "<CHOICE>Provisioned</CHOICE>"
                    + "<CHOICE>Canceled</CHOICE>"
                    + "<CHOICE>Error</CHOICE>"
                + "</CHOICES><Default>New (not requested)</Default></Field>";
        }

        /// <summary>
        /// List fields for the Site Collection requests list only
        /// If desired, modify display names and descriptions to fit a specific use case
        /// </summary>
        private static class SiteCollectionListFields
        {
            public static string SiteCollectionOwner = "<Field Name=\"SiteCollectionOwner\" DisplayName=\"Site Collection Owner\" Description=\"The administrator who should be set as the primary owner of this site collection.\""
                + " Type=\"User\" ID=\"{d048f2ec-ef31-4a79-8ad2-3c729bccca29}\" ShowField=\"ImnName\" UserSelectionMode=\"PeopleOnly\" Required=\"TRUE\"/>";

        }

        /// <summary>
        /// List fields for Project Subsites list only
        /// If desired, modify display names and descriptions to fit a specific use case
        /// </summary>
        private static class SubsiteListFields
        {

            public static string ProjectLeader = "<Field Name=\"ProjectLeader\" DisplayName = \"Project Leader\" Description=\"The Project Leader for this project.  This person will receive a notification when the project site has been provisioned.\""
                + " Type=\"User\" ID=\"{A0D54CE0-438F-42C9-99DA-A5E9961DB0A6}\" ShowField=\"ImnName\" UserSelectionMode=\"PeopleOnly\" Required=\"TRUE\"/>";

            public static string ProjectTeam = "<Field Name=\"ProjectTeam\" DisplayName = \"Project Team\" Description=\"The Project Team for this project, not including the Project Leader.  These people will receive notifications when the project site has been provisioned.\""
                + " Type=\"UserMulti\" Mult=\"TRUE\" ID=\"{C14697AD-FDDA-4EBB-9A61-AB7B00B3B3B3}\" ShowField=\"ImnName\" UserSelectionMode=\"PeopleOnly\" Required=\"FALSE\"/>";

            public static string ParentWeb = "<Field Name=\"ParentWeb\" DisplayName = \"Client Site\" Description=\"The client site underneath which to create this project site.\""
                + " Type=\"Lookup\" List=\"" + SiteCollectionListTitle + "\" ShowField=\"Title\" ID=\"{FBCB6409-11FB-48F0-A4EA-16171DE4D3F0}\" Required=\"TRUE\"/>";

        }

        /// <summary>
        /// Library fields for Site Templates library
        /// If desired, modify display names and descriptions to fit a specific use case
        /// </summary>
        private static class SiteTemplateLibraryFields
        {

            public static string Description = "<Field Name=\"SiteDescription\" DisplayName = \"Site Description\" Description=\"Describe this site template.\""
                + " Type=\"Text\" ID=\"{3E63F59F-A162-4DBC-B5B6-153F19336AA9}\" Required=\"TRUE\"/>";

            public static string BaseTemplate = "<Field Name=\"BaseTemplate\" DisplayName = \"Base Template\" Description=\"The base template for the site.\""
                + " Type=\"Choice\" ID=\"{67EF3125-DD00-475D-BD04-55EF85C34033}\" Required=\"TRUE\"><CHOICES>"
                //Add base template choices here
                + "<CHOICE>BLANKINTERNETCONTAINER#0</CHOICE>"//Publishing
                + "<CHOICE>PROJECTSITE#0</CHOICE>" //Project
                + "<CHOICE>STS#0</CHOICE>" //Team
                + "<CHOICE>COMMUNITY#0</CHOICE>" //Community
                + "<CHOICE>BLOG#0</CHOICE>" //Blog
                + "<CHOICE>WIKI#0</CHOICE>" //Wiki
                + "</CHOICES><Default>STS#0</Default></Field>";

            //public static string ApplyAdditionalConfiguration = "<Field Name=\"ApplyAdditionalConfiguration\" DisplayName = \"Apply Additional Configuration?\"" 
            //    + " Description=\"If yes, sites provisioned using this template will have additional configurations applied that are not specified in the template file, including security, home page, navigation and branding configurations.\""
            //    + " Type=\"Boolean\" ID=\"{9C88731C-783F-4054-BB92-35C6CCA1FA8A}\" Required=\"FALSE\"><Default>1</Default></Field>";
        }



        #endregion

        #region Public class variables - Set these prior to installation
        /// <summary>
        /// The list used to request subsites will have this title
        /// </summary>
        public static string SiteCollectionListTitle = "Site Collections";

        /// <summary>
        /// Description for the site collections list
        /// </summary>
        public static string SiteCollectionListDescription = "Manage requests for parent site collections";
        
        /// <summary>
        /// The list used to request subsites will be created with this title
        /// </summary>
        public static string SubsiteListTitle = "Subsites";

        /// <summary>
        /// Description for the subsites list
        /// </summary>
        public static string SubsiteListDescription = "Manage requests for subsites of parent site collections.";
        
        /// <summary>
        /// The library used to store site templates will be created with this title
        /// </summary>
        public static string SiteTemplateListTitle = "Site Templates";

        /// <summary>
        /// Description for the site templates library
        /// </summary>
        public static string SiteTemplateListDescription = "Manage site templates used to provision site collections and subsites.";

        #endregion

        #region Private methods

        /// <summary>
        /// Retrieve the CustomAction XML node from the file in the specified directory
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static XElement GetCustomActionXmlNode(string directory, string fileName)
        {

            //Source: https://msdn.microsoft.com/en-us/library/office/dn904536.aspx
            // The next line of code causes an exception to be thrown for files larger than 2 MB.
            string appDomain = HttpRuntime.AppDomainAppPath;

            string pathToFile = directory + "\\" + fileName;

            string fileUrl = Path.Combine(appDomain, pathToFile);


            XNamespace ns = "http://schemas.microsoft.com/sharepoint/";

            var xdoc = XDocument.Load(fileUrl);
            var customActionNode = xdoc.Element(ns + "Elements").Element(ns + "CustomAction");
            return customActionNode;
        }

        /// <summary>
        /// Adds a ScriptLink Custom Action, with the specified script file from the specified directory, to the current context web
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="directory"></param>
        /// <param name="scriptFileName"></param>
        private static void AddCustomScriptAction(ClientContext ctx, string directory, string scriptFileName, string customActionName)
        {

            Web web = ctx.Web;

            List assetLibrary = web.GetListByTitle("Site Assets");
            ctx.Load(assetLibrary, a => a.RootFolder);

            //Source: https://msdn.microsoft.com/en-us/library/office/dn904536.aspx
            // The next line of code causes an exception to be thrown for files larger than 2 MB.
            string appDomain = HttpRuntime.AppDomainAppPath;

            string pathToFile = directory + "\\" + scriptFileName;

            string fileUrl = Path.Combine(appDomain, pathToFile);


            // Use CSOM to uplaod the file in
            FileCreationInformation newFile = new FileCreationInformation();
            newFile.Content = System.IO.File.ReadAllBytes(fileUrl);
            newFile.Url = scriptFileName;
            newFile.Overwrite = true;
            Microsoft.SharePoint.Client.File uploadFile = assetLibrary.RootFolder.Files.Add(newFile);
            ctx.Load(uploadFile);
            ctx.ExecuteQuery();

            // Clean up existing actions that we may have deployed

            var existingActions = web.UserCustomActions;
            ctx.Load(existingActions);

            // Execute our uploads and initialzie the existingActions collection
            ctx.ExecuteQuery();


            //Clean up existing user action (make sure we don't duplicate
            var actions = existingActions.ToArray();
            foreach (var existingAction in actions)
            {
                if (existingAction.Name.Equals(customActionName, StringComparison.InvariantCultureIgnoreCase))
                    existingAction.DeleteObject();
            }
            ctx.ExecuteQuery();

            string linkUrl = assetLibrary.RootFolder.ServerRelativeUrl + "/" + scriptFileName;

            StringBuilder scripts = new StringBuilder(@"
                var headID = document.getElementsByTagName('head')[0]; 
                var");

            scripts.AppendFormat(@"
                newScript = document.createElement('script');
                newScript.type = 'text/javascript';
                newScript.src = '{0}';
                headID.appendChild(newScript);", linkUrl);
            string scriptBlock = scripts.ToString();

            //Build custom JS link
            string scriptLocation = "ScriptLink";
            int scriptSequence = 100;

            //Site
            UserCustomAction jsLink = web.UserCustomActions.Add();
            jsLink.Location = scriptLocation;
            jsLink.Sequence = scriptSequence;
            jsLink.ScriptBlock = scriptBlock;

            jsLink.Name = customActionName;

            jsLink.Update();
            ctx.ExecuteQuery();

        }

        /// <summary>
        /// Adds the Ribbon Action in the specified XML document, in the specified directory, to the list with the specified title.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="directory"></param>
        /// <param name="xmlDefinitionFileName"></param>
        /// <param name="listTitle"></param>
        /// <param name="ribbonActionName"></param>
        private static void AddCustomRibbonActionToListDefaultContentType(ClientContext ctx, string directory, string xmlDefinitionFileName, string listTitle, string ribbonActionName)
        {
            Web web = ctx.Web;

            #region Get list default content type ID
            List list = web.GetListByTitle(listTitle);
            ctx.Load(list, p => p.Id, p => p.ContentTypes);
            ctx.ExecuteQuery();

            var contentTypeId = list.ContentTypes[0].StringId;

            #endregion

            #region Ribbon custom action
            ctx.Load(web, w => w.UserCustomActions);

            XNamespace ns = "http://schemas.microsoft.com/sharepoint/";

            //Get XML element from single XML definition that we will use to create two identical custom actions
            var customActionNode = GetCustomActionXmlNode(directory, xmlDefinitionFileName);
            var commandUIExtensionNode = customActionNode.Element(ns + "CommandUIExtension");
            var xmlContent = commandUIExtensionNode.ToString();
            var location = customActionNode.Attribute("Location").Value;

            var registrationTypeString = customActionNode.Attribute("RegistrationType").Value;
            var registrationType = (UserCustomActionRegistrationType)Enum.Parse(typeof(UserCustomActionRegistrationType), registrationTypeString);

            var sequence = 1000;
            if (customActionNode.Attribute(ns + "Sequence") != null)
            {
                sequence = Convert.ToInt32(customActionNode.Attribute(ns + "Sequence").Value);
            }


            var existingActions = web.UserCustomActions;
            ctx.Load(existingActions);

            // Execute our uploads and initialize the existingActions collection
            ctx.ExecuteQuery();


            //Clean up existing user action (make sure we don't duplicate
            var actions = existingActions.ToArray();

            foreach (var existingAction in actions)
            {
                if (existingAction.Name.Equals(ribbonActionName, StringComparison.InvariantCultureIgnoreCase))
                    existingAction.DeleteObject();
            }
            ctx.ExecuteQuery();


            //Add custom action
            var ribbonAction = ctx.Web.UserCustomActions.Add();
            ribbonAction.RegistrationId = contentTypeId; // registrationId-- use the content type ID
            ribbonAction.Name = ribbonActionName;


            ribbonAction.Location = location;
            ribbonAction.CommandUIExtension = xmlContent; // CommandUIExtension xml
            ribbonAction.RegistrationType = registrationType;
            ribbonAction.Sequence = sequence;

            ribbonAction.Update();
            ctx.Load(ribbonAction);
            ctx.ExecuteQuery();


            #endregion
        }

        #endregion

       #region Public methods

        #region Installation Utilities

        /// <summary>
        /// Specify the site collection, subsite and template list titles and descriptions for a new app installation.  If you do not call this method, defaults will be used.
        /// </summary>
        /// <param name="siteCollectionListTitle"></param>
        /// <param name="siteCollectionListDescription"></param>
        /// <param name="subsiteListTitle"></param>
        /// <param name="subsiteListDescription"></param>
        /// <param name="siteTemplateListTitle"></param>
        /// <param name="siteTemplateListDescription"></param>
        public static void SetListDetails(string siteCollectionListTitle, string siteCollectionListDescription,
            string subsiteListTitle, string subsiteListDescription,
            string siteTemplateListTitle, string siteTemplateListDescription)
        {
            SiteCollectionListTitle = siteCollectionListTitle;
            SiteCollectionListDescription = siteCollectionListDescription;
            SubsiteListTitle = subsiteListTitle;
            SubsiteListDescription = subsiteListDescription;
            SiteTemplateListTitle = siteTemplateListTitle;
            SiteTemplateListDescription = siteTemplateListDescription;

        }


        /// <summary>
        /// Uploads the specified file to the library with the specified title in the host web, and sets the new list item's field values if possible.
        /// </summary>
        /// <param name="clientContext"></param>
        /// <param name="libraryTitle"></param>
        /// <param name="pathToFile"></param>
        /// <param name="fieldValues"></param>
        /// <returns></returns>
        public static string UploadFileToLibrary(ClientContext clientContext, string libraryTitle, string pathToFile, Dictionary<string, string> fieldValues = null)
        {
            string serverRelativeUrl = string.Empty;

            Web web = clientContext.Web;

            List library = web.Lists.GetByTitle(libraryTitle);


            //Source: https://msdn.microsoft.com/en-us/library/office/dn904536.aspx
            FileCreationInformation newFile = new FileCreationInformation();

            // The next line of code causes an exception to be thrown for files larger than 2 MB.
            string appDomain = HttpRuntime.AppDomainAppPath;
            string fileUrl = Path.Combine(appDomain, pathToFile);
            newFile.Content = System.IO.File.ReadAllBytes(fileUrl);
            newFile.Url = System.IO.Path.GetFileName(fileUrl);
            newFile.Overwrite = true;

            // Add file to the library.
            try
            {

                Microsoft.SharePoint.Client.File uploadFile = library.RootFolder.Files.Add(newFile);
                clientContext.Load(uploadFile);

                if (fieldValues != null)
                {
                    //Set metadata
                    foreach (var field in fieldValues.Keys)
                    {
                        uploadFile.ListItemAllFields[field] = fieldValues[field];
                    }

                    uploadFile.ListItemAllFields.Update();
                }
                clientContext.ExecuteQuery();
                serverRelativeUrl = uploadFile.ServerRelativeUrl;

            }
            catch (ServerException ex)
            {
                throw new Exception(String.Format("Error uploading file '{0}' to library '{1}': {2}", pathToFile, libraryTitle, ex.Message));
                //Swallow; file already exists and we don't want to overwrite
                //TODO: Log
            }

            return serverRelativeUrl;
        }

        /// <summary>
        /// Checks if the 3 required lists exist on the host web
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public static bool RequiredListsExist(ClientContext ctx)
        {
            return ctx.Web.ListExists(SiteTemplateListTitle)
                && ctx.Web.ListExists(SiteCollectionListTitle)
                && ctx.Web.ListExists(SubsiteListTitle);
        }

        /// <summary>
        /// Adds all custom actions to the current context web using the specified files in the specified directory.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="directory"></param>
        /// <param name="scriptFileName"></param>
        /// <param name="siteCollectionListTitle"></param>
        /// <param name="siteCollectionRibbonActionFileName"></param>
        /// <param name="subsiteListTitle"></param>
        /// <param name="subsiteRibbonActionFileName"></param>
        public static void AddCustomActions(ClientContext ctx, string directory, string scriptFileName, string siteCollectionListTitle, string siteCollectionRibbonActionFileName,
            string subsiteListTitle, string subsiteRibbonActionFileName, string jQueryScriptFileName="jquery-1.11.2.min.js")
        {
            //jQuery
            //AddCustomScriptAction(ctx, directory, jQueryScriptFileName, JsJQueryActionName);

            //Script
            AddCustomScriptAction(ctx, directory, scriptFileName, JsLinkActionName);

            //Site Collection List - Ribbon
            AddCustomRibbonActionToListDefaultContentType(ctx, directory, siteCollectionRibbonActionFileName, siteCollectionListTitle, SiteCollRibbonActionName);

            //Subsites List - Ribbon
            AddCustomRibbonActionToListDefaultContentType(ctx, directory, subsiteRibbonActionFileName, subsiteListTitle, SubsiteRibbonActionName);

        }

        #endregion

        #region Uninstallation Utilities
        /// <summary>
        /// Deletes the lists used by this app.  To be called from AppUninstalling
        /// </summary>
        /// <param name="clientContext"></param>
        public static void DeleteLists(ClientContext clientContext)
        {
            Web web = clientContext.Web;
            clientContext.Load(web);

            //Delete Project Sites List
            try
            {

                if (clientContext.Web.ListExists(SubsiteListTitle))
                {
                    List siteRequests = clientContext.Web.Lists.GetByTitle(SubsiteListTitle);
                    siteRequests.DeleteObject();
                    clientContext.ExecuteQuery();
                }
            }
            catch (Exception ex)
            {
                //Continue; uninstalling
                //TODO: Log
            }

            //Delete Sites List
            try
            {

                if (clientContext.Web.ListExists(SiteCollectionListTitle))
                {
                    List siteRequests = clientContext.Web.Lists.GetByTitle(SiteCollectionListTitle);
                    siteRequests.DeleteObject();
                    clientContext.ExecuteQuery();
                }
            }
            catch (Exception ex)
            {
                //Continue; uninstalling
                //TODO: Log
            }

            //Delete Site Templates List
            try
            {
                if (clientContext.Web.ListExists(SiteTemplateListTitle))
                {
                    List siteRequests = clientContext.Web.Lists.GetByTitle(SiteTemplateListTitle);
                    siteRequests.DeleteObject();
                    clientContext.ExecuteQuery();
                }
            }
            catch (Exception ex)
            {
                //Continue; uninstalling
                //TODO: Log
            }
        }

        /// <summary>
        /// Remove the custom actions from the site; to be called on AppUninstalling
        /// </summary>
        /// <param name="ctx"></param>
        public static void RemoveCustomActions(ClientContext ctx)
        {
            Web web = ctx.Web;
            ctx.Load(web, w => w.UserCustomActions);
            ctx.ExecuteQuery();

            var existingActions = web.UserCustomActions;
            ctx.Load(existingActions);

            // Execute our uploads and initialzie the existingActions collection
            ctx.ExecuteQuery();


            //Clean up existing user action (make sure we don't duplicate
            var actions = existingActions.ToArray();

            // Clean up existing actions that we may have deployed
            foreach (var existingAction in actions)
            {
                if (existingAction.Name.Equals(SiteCollRibbonActionName, StringComparison.InvariantCultureIgnoreCase)
                    || existingAction.Name.Equals(SubsiteRibbonActionName, StringComparison.InvariantCultureIgnoreCase)
                    || existingAction.Name.Equals(JsLinkActionName, StringComparison.InvariantCultureIgnoreCase))
                    existingAction.DeleteObject();
            }
            ctx.ExecuteQuery();

        }

        #endregion

        #region List Creation

        /// <summary>
        /// Create the library for storing site template XML files.
        /// </summary>
        /// <param name="ctx"></param>
        public static void CreateTemplateLibrary(ClientContext ctx, string iconUrl = "")
        {
            if (ctx != null)
            {
                Web web = ctx.Web;
                List templateLibrary;

                if (!ctx.Web.ListExists(SiteTemplateListTitle))
                {
                    try
                    {

                        templateLibrary = web.CreateList(ListTemplateType.DocumentLibrary, SiteTemplateListTitle, false);

                        //templateLibrary.Hidden = true;
                        //templateLibrary.Update();
                        ctx.ExecuteQuery();
                    }

                    catch (Exception ex)
                    {
                        //For now, swallow; list already exists
                        //TODO: Log
                    }

                    //Get reference to newly-created or existing list
                    try
                    {
                        templateLibrary = web.Lists.GetByTitle(SiteTemplateListTitle);

                        //Set list description
                        ctx.Load(templateLibrary, t => t.Description, t => t.ImageUrl);
                        templateLibrary.Description = SiteTemplateListDescription;

                        //Set list icon
                        if (!String.IsNullOrEmpty(iconUrl))
                        {
                            templateLibrary.ImageUrl = iconUrl;
                            templateLibrary.Update();


                        }

                        ctx.ExecuteQuery();

                        //Add all fields to be created to this collection
                        StringCollection fields = new StringCollection{
                        SiteTemplateLibraryFields.Description,
                        SiteTemplateLibraryFields.BaseTemplate
                        };

                        //Create fields
                        foreach (string field in fields)
                        {
                            try
                            {
                                templateLibrary.CreateField(field);
                            }
                            catch (Exception ex)
                            {
                                //For now, swallow; field could not be created or already exists
                                //TODO: Log
                            }
                        }

                        ctx.ExecuteQuery();


                        //List default view
                        ctx.Load(templateLibrary, l => l.DefaultView, l => l.Fields);
                        templateLibrary.DefaultView.ViewFields.Add("Title");
                        templateLibrary.DefaultView.ViewFields.Add("Site Description");
                        templateLibrary.DefaultView.ViewFields.Add("Base Template");

                        templateLibrary.DefaultView.Update();

                        templateLibrary.Update();
                        ctx.ExecuteQueryRetry();



                    }
                    catch (Exception ex)
                    {
                        //List doesn't exist; log
                        //throw new Exception("The Site Templates list could not be created: " + ex.Message);
                    }
                }
            }

        }


        /// <summary>
        /// Create the list for storing site collection requests
        /// </summary>
        /// <param name="ctx"></param>
        public static void CreateSiteCollectionList(ClientContext ctx, string iconUrl = "")
        {
            if (ctx != null)
            {
                Web web = ctx.Web;
                List siteCollectionList;

                if (!ctx.Web.ListExists(SiteCollectionListTitle))
                {

                    //Try creating the list
                    try
                    {

                        siteCollectionList = web.CreateList(ListTemplateType.GenericList, SiteCollectionListTitle, false);
                        ctx.ExecuteQuery();
                    }

                    catch (Exception ex)
                    {
                        //For now, swallow; list already exists
                        //TODO: Log
                    }


                    //Get reference to newly-created or existing list
                    try
                    {
                        siteCollectionList = web.Lists.GetByTitle(SiteCollectionListTitle);

                        ctx.Load(siteCollectionList, l => l.Description, l => l.ImageUrl);
                        ctx.ExecuteQuery();

                        siteCollectionList.Description = SiteCollectionListDescription;

                        //Set list icon
                        if (!String.IsNullOrEmpty(iconUrl))
                        {
                            siteCollectionList.ImageUrl = iconUrl;
                            siteCollectionList.Update();
                        }

                        ctx.ExecuteQuery();

                        //Create URL field (must be unique; EnforceUniqueValues must be enabled programmatically)
                        try
                        {
                            Field urlField = siteCollectionList.CreateField(SharedListFields.Abbreviation);
                            urlField.EnforceUniqueValues = true;
                            urlField.Indexed = true;
                            ctx.Load(urlField);
                            urlField.Update();
                            ctx.ExecuteQuery();
                        }
                        catch (Exception ex)
                        {
                            //TODO: Log
                        }

                        //Add all fields to be created to this collection
                        StringCollection fields = new StringCollection {
                        
                        SharedListFields.Processed
                        , SiteCollectionListFields.SiteCollectionOwner
                        , SharedListFields.LinkToProvisionedSite
                        , SharedListFields.ProvisioningStatus
                        , SharedListFields.ErrorMessage
                        
                    };

                        //Create base fields
                        foreach (string field in fields)
                        {
                            try
                            {
                                siteCollectionList.CreateField(field);
                            }
                            catch (Exception ex)
                            {
                                //For now, swallow; field could not be created or already exists
                                //TODO: Log
                            }
                        }

                        //Create lookup field for template
                        if (web.ListExists(SiteTemplateListTitle))
                        {
                            try
                            {
                                var siteTemplatesLibrary = web.Lists.GetByTitle(SiteTemplateListTitle);
                                ctx.Load(siteTemplatesLibrary);
                                ctx.ExecuteQuery();

                                var id = siteTemplatesLibrary.Id;
                                var lookupStr = "List=\"{" + id.ToString() + "}\"";
                                var fieldDef = SharedListFields.SiteTemplate.Replace("List=\"" + SiteTemplateListTitle + "\"", lookupStr);

                                siteCollectionList.CreateField(fieldDef);
                            }
                            catch (Exception ex)
                            {
                                //For now, swallow; field could not be created or already exists
                                //TODO: Log
                            }

                            ctx.ExecuteQuery();
                        }


                        //List default view
                        ctx.Load(siteCollectionList, l => l.DefaultView, l => l.Fields, l => l.ContentTypes);
                        ctx.ExecuteQuery();

                        siteCollectionList.DefaultView.ViewFields.Add("Abbreviation");
                        siteCollectionList.DefaultView.ViewFields.Add("Site Template");
                        siteCollectionList.DefaultView.ViewFields.Add("Provisioning Status");
                        siteCollectionList.DefaultView.ViewFields.Add("Processed by Provisioning Job");
                        siteCollectionList.DefaultView.ViewFields.Add("Link to Site");
                        siteCollectionList.DefaultView.ViewFields.Add("Error");
                        siteCollectionList.DefaultView.Update();



                        ContentType ct = siteCollectionList.ContentTypes[0];
                        siteCollectionList.ContentTypesEnabled = true;
                        ct.Name = "Client Site Collection";
                        ct.Update(false);
                        ctx.Load(ct);
                        siteCollectionList.Update();
                        ctx.ExecuteQueryRetry();


                    }
                    catch (Exception ex)
                    {
                        //Bubble up;
                        throw;
                    }
                }
                else
                {
                    //Log and move on.
                }

            }

        }


        /// <summary>
        /// Creates the list for storing subsite requests
        /// </summary>
        /// <param name="ctx"></param>
        public static void CreateSubsiteList(ClientContext ctx, string iconUrl = "")
        {
            if (ctx != null)
            {
                Web web = ctx.Web;
                List subsiteList;

                if (!web.ListExists(SubsiteListTitle))
                {

                    //Try creating the list
                    try
                    {

                        subsiteList = web.CreateList(ListTemplateType.GenericList, SubsiteListTitle, false);
                        ctx.ExecuteQuery();
                    }

                    catch (Exception ex)
                    {
                        //For now, swallow; list already exists
                        //TODO: Log
                    }

                    //Get reference to newly-created or existing list
                    try
                    {
                        subsiteList = web.Lists.GetByTitle(SubsiteListTitle);

                        ctx.Load(subsiteList, l => l.Description, l => l.ImageUrl);
                        ctx.ExecuteQuery();

                        subsiteList.Description = SubsiteListDescription;

                        //Set list icon
                        if (!String.IsNullOrEmpty(iconUrl))
                        {
                            subsiteList.ImageUrl = iconUrl;
                            subsiteList.Update();
                        }

                        ctx.ExecuteQuery();


                        //Add all fields to be created to this collection
                        StringCollection fields = new StringCollection {
                        SharedListFields.Abbreviation
                        , SubsiteListFields.ProjectLeader
                        , SubsiteListFields.ProjectTeam
                        , SharedListFields.Processed
                        , SharedListFields.LinkToProvisionedSite
                        , SharedListFields.ProvisioningStatus
                        , SharedListFields.ErrorMessage
                        
                        };

                        //Create base fields
                        foreach (string field in fields)
                        {
                            try
                            {
                                subsiteList.CreateField(field);

                            }
                            catch (Exception ex)
                            {
                                //For now, swallow; field could not be created or already exists
                                //TODO: Log
                            }
                        }

                        //Create lookup field for template
                        if (web.ListExists(SiteTemplateListTitle))
                        {
                            try
                            {
                                var siteTemplatesLibrary = web.Lists.GetByTitle(SiteTemplateListTitle);
                                ctx.Load(siteTemplatesLibrary, s => s.Id);
                                ctx.ExecuteQuery();

                                var id = siteTemplatesLibrary.Id;
                                var lookupStr = "List=\"{" + id.ToString() + "}\"";
                                var fieldDef = SharedListFields.SiteTemplate.Replace("List=\"" + SiteTemplateListTitle + "\"", lookupStr);

                                subsiteList.CreateField(fieldDef);
                            }
                            catch (Exception ex)
                            {
                                //For now, swallow; field could not be created or already exists
                                //TODO: Log
                            }

                        }


                        //Create lookup field for client site
                        //Create lookup field for template

                        if (web.ListExists(SiteCollectionListTitle))
                        {
                            try
                            {
                                var clientSitesLibrary = web.Lists.GetByTitle(SiteCollectionListTitle);
                                ctx.Load(clientSitesLibrary, c => c.Id);
                                ctx.ExecuteQuery();
                                var id = clientSitesLibrary.Id;

                                var lookupStr = "List=\"{" + id.ToString() + "}\"";
                                var fieldDef = SubsiteListFields.ParentWeb.Replace("List=\"" + SiteCollectionListTitle + "\"", lookupStr);

                                subsiteList.CreateField(fieldDef);

                            }

                            catch (Exception ex)
                            {
                                //For now, swallow; field could not be created or already exists
                                //TODO: Log
                            }
                        }
                        else
                        {
                            //Log and move on
                        }


                        ctx.ExecuteQuery();


                        //List default view
                        ctx.Load(subsiteList, l => l.DefaultView, l => l.Fields, l => l.ContentTypes);

                        subsiteList.DefaultView.ViewFields.Add("Abbreviation");
                        subsiteList.DefaultView.ViewFields.Add("Client Site");
                        subsiteList.DefaultView.ViewFields.Add("Site Template");
                        subsiteList.DefaultView.ViewFields.Add("Project Leader");
                        subsiteList.DefaultView.ViewFields.Add("Project Team");
                        subsiteList.DefaultView.ViewFields.Add("Provisioning Status");
                        subsiteList.DefaultView.ViewFields.Add("Processed by Provisioning Job");
                        subsiteList.DefaultView.ViewFields.Add("Link to Site");
                        subsiteList.DefaultView.ViewFields.Add("Error");
                        subsiteList.DefaultView.Update();
                        subsiteList.Update();
                        ctx.ExecuteQuery();

                        ContentType ct = subsiteList.ContentTypes[0];
                        subsiteList.ContentTypesEnabled = true;
                        ct.Name = "Project Subsite";
                        ct.Update(false);
                        ctx.Load(ct);
                        subsiteList.Update();
                        ctx.ExecuteQuery();



                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                }
                else
                {
                    //Log and move on
                }





            }

        }

        #endregion

        #endregion 

        
    }
}