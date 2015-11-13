using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;

namespace CloudProvisioningWeb.Common
{
    public class AppInstallationHelper
    {

        /// <summary>
        /// The list used to request subsites will have this title
        /// </summary>
        private const string SiteCollectionListTitle = "Client Sites";
        
        /// <summary>
        /// The list used to request subsites will be created with this title
        /// </summary>
        private const string SubsiteListTitle = "Project Sites";
        
        /// <summary>
        /// The library used to store site templates will be created with this title
        /// </summary>
        private const string SiteTemplateListTitle = "Site Templates";

        /// <summary>
        /// List fields for Site Collections list
        /// If desired, modify display names and descriptions to fit a specific use case
        /// </summary>
        private static class SiteCollectionsListFields
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
                + " Type=\"URL\" Format=\"Hyperlink\" ID=\"{210C37AC-A2BC-4463-9862-B54D443FB4AF}\" ShowInEditForm=\"TRUE\" ShowInNewForm=\"FALSE\"/>";

            public static string ProvisioningStatus = "<Field Name=\"ProvisioningStatus\" DisplayName=\"Provisioning Status\" Description=\"This status is updated by the provisioning engine once a request is submitted.\""
                + " Type=\"Choice\" ID=\"{E24E549F-44A6-403A-B86A-0EFD88719611}\" ShowInEditForm=\"TRUE\" ShowInNewForm=\"FALSE\"><CHOICES>"
                    + "<CHOICE>New (not requested)</CHOICE>"
                    + "<CHOICE>Requested</CHOICE>"
                    + "<CHOICE>Provisioning...</CHOICE>"
                    + "<CHOICE>Provisioned</CHOICE>"
                    + "<CHOICE>Error</CHOICE>"
                + "</CHOICES><Default>New (not requested)</Default></Field>";
        }

        /// <summary>
        /// List fields for Project Sites list
        /// If desired, modify display names and descriptions to fit a specific use case
        /// </summary>
        private static class SubsiteListFields
        {

            public static string Abbreviation = "<Field Name=\"Abbreviation\" DisplayName = \"Abbreviation\" Description=\"The abbreviation for the client or project name, to be used for the URL of the new site; this is the part after the slash (/).  Should not contain spaces.  If a value for this is not provided, the client or project name will be used with hyphens (-) replacing spaces.\""
                + " Type=\"Text\" ID=\"{DF7C4DF8-7CC4-405C-BD7A-7575EDCE06A6}\" Required=\"FALSE\"/>";

            public static string SiteOwner = "<Field Name=\"SiteOwner\" DisplayName = \"Project Leader\" Description=\"The Project Leader for this project.  This person will receive a notification when the project site has been provisioned.\""
                + " Type=\"User\" ID=\"{A0D54CE0-438F-42C9-99DA-A5E9961DB0A6}\" ShowField=\"ImnName\" UserSelectionMode=\"PeopleOnly\" Required=\"TRUE\"/>";

            public static string SiteMembers = "<Field Name=\"SiteMembers\" DisplayName = \"Project Team\" Description=\"The Project Team for this project, not including the Project Leader.  These people will receive notifications when the project site has been provisioned.\""
                + " Type=\"User\" ID=\"{C14697AD-FDDA-4EBB-9A61-AB7B00B3B3B3}\" ShowField=\"ImnName\" UserSelectionMode=\"PeopleOnly\" Required=\"FALSE\"/>";

            public static string Processed = "<Field Name=\"Processed\" DisplayName = \"Processed by Provisioning Job\" Description=\"The date and time this site was processed by the provisioning job.\""
                + " Type=\"DateTime\" ID=\"{D1B8CF60-9159-43A8-A9D5-8BA36C803285}\" Required=\"FALSE\" ShowInEditForm=\"FALSE\" ShowInNewForm=\"FALSE\"/>";

            public static string ErrorMessage = "<Field Name=\"ErrorMessage\" DisplayName = \"Error\" Description=\"If an error occurs during site provisioning, it will be logged here.\""
                + " Type=\"Text\" ID=\"{A04E7624-EB0A-4F46-A4C0-4703AA585E9A}\" Required=\"FALSE\" ShowInEditForm=\"FALSE\" ShowInNewForm=\"FALSE\"/>";

            public static string SiteTemplate = "<Field Name=\"SiteTemplate\" DisplayName = \"Site Template\" Description=\"The site template to use for this site.\""
                + " Type=\"Lookup\" List=\"" + SiteTemplateListTitle + "\" ShowField=\"Title\" ID=\"{6D89533E-2F72-4593-A954-E5A3B70EB966}\" Required=\"TRUE\"/>";

            public static string ParentWeb = "<Field Name=\"ParentWeb\" DisplayName = \"Client Site\" Description=\"The client site underneath which to create this project site.\""
                + " Type=\"Lookup\" List=\"" + SiteCollectionListTitle + "\" ShowField=\"Title\" ID=\"{FBCB6409-11FB-48F0-A4EA-16171DE4D3F0}\" Required=\"TRUE\"/>";

            public static string LinkToProvisionedSite = "<Field Name=\"LinkToProvisionedSite\" DisplayName=\"Link to Site\" Description=\"Click here to open the site.\""
                + " Type=\"URL\" Format=\"Hyperlink\" ID=\"{9E133A6A-B61E-4F91-98BC-86AD505F000C}\" ShowInEditForm=\"TRUE\" ShowInNewForm=\"FALSE\"/>";

            public static string ProvisioningStatus = "<Field Name=\"ProvisioningStatus\" DisplayName=\"Provisioning Status\" Description=\"This status is updated by the provisioning engine once a request is submitted.\""
                + " Type=\"Choice\" ID=\"{A166B609-22B3-41AF-A88F-BAE6E01D9FF9}\" ShowInEditForm=\"TRUE\" ShowInNewForm=\"FALSE\"><CHOICES>"
                    + "<CHOICE>New (not requested)</CHOICE>"
                    + "<CHOICE>Requested</CHOICE>"
                    + "<CHOICE>Provisioning...</CHOICE>"
                    + "<CHOICE>Provisioned</CHOICE>"
                    + "<CHOICE>Error</CHOICE>"
                + "</CHOICES><Default>New (not requested)</Default></Field>";
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
        }


        /// <summary>
        /// Uploads a file provisioned as an asset with this app, to a document library on the host web
        /// </summary>
        /// <param name="clientContext"></param>
        /// <param name="libraryTitle"></param>
        /// <param name="pathToFile"></param>
        /// <param name="fieldValues"></param>
        public static void UploadFileToLibrary(ClientContext clientContext, string libraryTitle, string pathToFile, Dictionary<string, string> fieldValues)
        {

            Web web = clientContext.Web;

            List library = web.Lists.GetByTitle(libraryTitle);


            //Source: https://msdn.microsoft.com/en-us/library/office/dn904536.aspx
            FileCreationInformation newFile = new FileCreationInformation();

            // The next line of code causes an exception to be thrown for files larger than 2 MB.
            string appDomain = HttpRuntime.AppDomainAppPath;
            string fileUrl = Path.Combine(appDomain, pathToFile);
            newFile.Content = System.IO.File.ReadAllBytes(fileUrl);
            newFile.Url = System.IO.Path.GetFileName(fileUrl);



            // Add file to the library.
            try
            {
                Microsoft.SharePoint.Client.File uploadFile = library.RootFolder.Files.Add(newFile);
                clientContext.Load(uploadFile);

                //Set metadata
                foreach (var field in fieldValues.Keys)
                {
                    uploadFile.ListItemAllFields[field] = fieldValues[field];
                }

                uploadFile.ListItemAllFields.Update();

                clientContext.ExecuteQuery();
            }
            catch (ServerException ex)
            {
                //Swallow; file already exists and we don't want to overwrite
                //TODO: Log
            }
        }

        /// <summary>
        /// Deletes the lists used by this app.  Called from AppUninstalling
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
        /// Creates a library for storing site template XML files.
        /// </summary>
        /// <param name="ctx"></param>
        public static void CreateTemplateLibrary(ClientContext ctx)
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
        /// Creates a list for storing site requests
        /// </summary>
        /// <param name="ctx"></param>
        public static void CreateClientSiteList(ClientContext ctx)
        {
            if (ctx != null)
            {
                Web web = ctx.Web;
                List clientSiteList;

                if (!ctx.Web.ListExists("Client Sites"))
                {

                    //Try creating the list
                    try
                    {

                        clientSiteList = web.CreateList(ListTemplateType.GenericList, SiteCollectionListTitle, false);
                        //clientSiteList.Hidden = true;
                        //clientSiteList.Update();
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
                        clientSiteList = web.Lists.GetByTitle(SiteCollectionListTitle);
                        
                        //Create URL field (must be unique; EnforceUniqueValues must be enabled programmatically)
                        try
                        {
                            Field urlField = clientSiteList.CreateField(SiteCollectionsListFields.Abbreviation);
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
                        
                        SiteCollectionsListFields.Processed
                        , SiteCollectionsListFields.LinkToProvisionedSite
                        , SiteCollectionsListFields.ProvisioningStatus
                        //, ClientSitesListFields.ErrorOccurred
                        , SiteCollectionsListFields.ErrorMessage
                        
                    };

                        //Create base fields
                        foreach (string field in fields)
                        {
                            try
                            {
                                clientSiteList.CreateField(field);
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
                                var fieldDef = SiteCollectionsListFields.SiteTemplate.Replace("List=\"" + SiteTemplateListTitle + "\"", lookupStr);

                                clientSiteList.CreateField(fieldDef);
                            }
                            catch (Exception ex)
                            {
                                //For now, swallow; field could not be created or already exists
                                //TODO: Log
                            }

                            ctx.ExecuteQuery();
                        }


                        //List default view
                        ctx.Load(clientSiteList, l => l.DefaultView, l=>l.Fields);

                        clientSiteList.DefaultView.ViewFields.Add("Abbreviation");
                        clientSiteList.DefaultView.ViewFields.Add("Site Template");
                        clientSiteList.DefaultView.ViewFields.Add("Provisioning Status");
                        clientSiteList.DefaultView.ViewFields.Add("Processed by Provisioning Job");
                        clientSiteList.DefaultView.ViewFields.Add("Link to Site");
                        clientSiteList.DefaultView.ViewFields.Add("Error");
                        clientSiteList.DefaultView.Update();
                        clientSiteList.Update();
                        ctx.ExecuteQueryRetry();
                    }
                    catch (Exception ex)
                    {
                        //List doesn't exist; throw
                        //throw new Exception("The Client Sites list could not be created: " + ex.Message);
                    }
                }
                else
                {
                    //throw new Exception("There is already a list called Client Sites on this site.  Please remove the list and try installing again.");
                }




            }

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
        /// Creates a list for storing site requests
        /// </summary>
        /// <param name="ctx"></param>
        public static void CreateProjectSiteList(ClientContext ctx)
        {
            if (ctx != null)
            {
                Web web = ctx.Web;
                List projectSiteList;

                if (!web.ListExists(SubsiteListTitle))
                {

                    //Try creating the list
                    try
                    {

                        projectSiteList = web.CreateList(ListTemplateType.GenericList, SubsiteListTitle, false);
                        //projectSiteList.Hidden = true;
                        //projectSiteList.Update();
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
                        projectSiteList = web.Lists.GetByTitle(SubsiteListTitle);

                        //Create URL field (must be unique; EnforceUniqueValues must be enabled programmatically)
                        try
                        {
                            Field urlField = projectSiteList.CreateField(SubsiteListFields.Abbreviation);
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
                        
                        SubsiteListFields.SiteOwner
                        , SubsiteListFields.SiteMembers
                        , SubsiteListFields.Processed
                        , SubsiteListFields.LinkToProvisionedSite
                        , SubsiteListFields.ProvisioningStatus
                        , SubsiteListFields.ErrorMessage
                        
                        };

                        //Create base fields
                        foreach (string field in fields)
                        {
                            try
                            {
                                projectSiteList.CreateField(field);

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
                                var fieldDef = SubsiteListFields.SiteTemplate.Replace("List=\"" + SiteTemplateListTitle + "\"", lookupStr);

                                projectSiteList.CreateField(fieldDef);
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
                                ctx.Load(clientSitesLibrary);
                                ctx.ExecuteQuery();
                                var id = clientSitesLibrary.Id;

                                var lookupStr = "List=\"{" + id.ToString() + "}\"";
                                var fieldDef = SubsiteListFields.ParentWeb.Replace("List=\"" + SiteCollectionListTitle + "\"", lookupStr);

                                projectSiteList.CreateField(fieldDef);

                            }

                            catch (Exception ex)
                            {
                                //For now, swallow; field could not be created or already exists
                                //TODO: Log
                            }
                        }
                        else
                        {
                            //throw new Exception("Client Sites list doesn't exist.");
                        }


                        ctx.ExecuteQuery();


                        //List default view
                        ctx.Load(projectSiteList, l => l.DefaultView, l => l.Fields);

                        projectSiteList.DefaultView.ViewFields.Add("Abbreviation");
                        projectSiteList.DefaultView.ViewFields.Add("Site Template");
                        projectSiteList.DefaultView.ViewFields.Add("Project Leader");
                        projectSiteList.DefaultView.ViewFields.Add("Project Team");
                        projectSiteList.DefaultView.ViewFields.Add("Provisioning Status");
                        projectSiteList.DefaultView.ViewFields.Add("Processed by Provisioning Job");
                        projectSiteList.DefaultView.ViewFields.Add("Link to Site");
                        projectSiteList.DefaultView.ViewFields.Add("Error");
                        projectSiteList.DefaultView.Update();
                        projectSiteList.Update();
                        ctx.ExecuteQueryRetry();
                    }
                    catch (Exception ex)
                    {
                        //List doesn't exist; throw
                        //throw new Exception("The Project Sites list could not be created: " + ex.Message);
                    }
                }
                else
                {
                    // throw new Exception("There is already a list called Project Sites on this site.  Please remove the list and try installing again.");
                }





            }

        }

    }
}