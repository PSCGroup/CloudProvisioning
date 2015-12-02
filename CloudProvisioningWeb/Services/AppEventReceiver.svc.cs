using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using CloudProvisioningWeb.Common;
using System.Configuration;

namespace CloudProvisioningWeb.Services
{
    public class AppEventReceiver : IRemoteEventService
    {

        private const string Key = "CloudProvisioningInstalling";
        private bool IsInstalling(ClientContext ctx)
        {
            
            if (!ctx.Web.PropertyBagContainsKey(Key))
            {
                return false;
            }
            else
            {
                if (ctx.Web.GetPropertyBagValueInt(Key, 0) == 1)
                    return true;
                else
                    return false;
            }
        }

        private void SetInstalling(bool isInstalling, ClientContext ctx)
        {
            int bit = 0;
            if (isInstalling)
                bit = 1;
            ctx.Web.SetPropertyBagValue(Key, bit);
        }

        
        /// <summary>
        /// Handles app events that occur after the app is installed or upgraded, or when app is being uninstalled.
        /// </summary>
        /// <param name="properties">Holds information about the app event.</param>
        /// <returns>Holds information returned from the app event.</returns>
        public SPRemoteEventResult ProcessEvent(SPRemoteEventProperties properties)
        {
            SPRemoteEventResult result = new SPRemoteEventResult();

            switch (properties.EventType)
            {

                case SPRemoteEventType.AppInstalled:
                    {
                        using (ClientContext ctx = TokenHelper.CreateAppEventClientContext(properties, useAppWeb: false))
                        {
                            try
                            {
                                if (ctx != null)
                                {

                                    //temp
                                    //ctx.Web.RemovePropertyBagValue(Key);
                                    //throw new Exception();

                                    if (!IsInstalling(ctx))
                                    {
                                        SetInstalling(true, ctx);

                                        //Set values before installation based on web.config
                                        

                                        var web = ctx.Web;
                                        ctx.Load(web, w => w.Url);
                                        ctx.ExecuteQuery();

                                        //Set list instance details for installing, based on web.config
                                        SetListInstanceDetails();


                                        //Upload large icons
                                        AppInstallationHelper.UploadFileToLibrary(ctx, "Site Assets", "Icons\\LSiteIcon.png");
                                        AppInstallationHelper.UploadFileToLibrary(ctx, "Site Assets", "Icons\\LSubsiteIcon.png");
                                        AppInstallationHelper.UploadFileToLibrary(ctx, "Site Assets", "Icons\\LTemplatesIcon.png");
                                        
                                        //Upload jQuery
                                        AppInstallationHelper.UploadFileToLibrary(ctx, "Site Assets", "Scripts\\jquery-1.10.2.min.js");

                                        //Upload small icons - get reference to URLs
                                        string scIconPath = AppInstallationHelper.UploadFileToLibrary(ctx, "Site Assets", "Icons\\ISiteIcon.png");
                                        string subIconPath = AppInstallationHelper.UploadFileToLibrary(ctx, "Site Assets", "Icons\\ISubsiteIcon.png");
                                        string templatesIconPath = AppInstallationHelper.UploadFileToLibrary(ctx, "Site Assets", "Icons\\ITemplatesIcon.png");

                                        //Create template library
                                        AppInstallationHelper.CreateTemplateLibrary(ctx, templatesIconPath);

                                        Dictionary<string, string> fieldValues_PSCClientTemplate = new Dictionary<string, string>();
                                        fieldValues_PSCClientTemplate.Add("BaseTemplate", "STS#0");
                                        fieldValues_PSCClientTemplate.Add("SiteDescription", "A PSC client site collection.");

                                        AppInstallationHelper.UploadFileToLibrary(ctx, "Site Templates", "Templates\\Client Site Collection.xml", fieldValues_PSCClientTemplate);


                                        //Upload PSC project site template
                                        Dictionary<string, string> fieldValues_PSCProjectTemplate = new Dictionary<string, string>();
                                        fieldValues_PSCProjectTemplate.Add("BaseTemplate", "STS#0");
                                        fieldValues_PSCProjectTemplate.Add("SiteDescription", "A PSC project sub-site beneath a PSC Client site collection.");

                                        AppInstallationHelper.UploadFileToLibrary(ctx, "Site Templates", "Templates\\Project Subsite.xml", fieldValues_PSCProjectTemplate);

                                        //Create Client Site Collections list
                                        AppInstallationHelper.CreateSiteCollectionList(ctx, scIconPath);

                                        //Create Project Subsites list
                                        AppInstallationHelper.CreateSubsiteList(ctx, subIconPath);

                                        //Install custom actions
                                        AppInstallationHelper.AddCustomActions(ctx, "CustomActions", "CustomActionScript.js", "Client Site Collections", "CustomActionDefinition_SiteColl.xml", "Project Subsites", "CustomActionDefinition_Subsite.xml");
                                        

                                        SetInstalling(false, ctx);
                                        ctx.Web.RemovePropertyBagValue(Key);
                                        result.Status = SPRemoteEventServiceStatus.Continue;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {

                                result.ErrorMessage = ex.Message;
                                SetInstalling(false, ctx);
                                result.Status = SPRemoteEventServiceStatus.CancelWithError;


                            }

                            finally
                            {
                                
                            }
                        }



                    }

                    break;
                case SPRemoteEventType.AppUpgraded:
                    break;
                case SPRemoteEventType.AppUninstalling:
                    {
                        
                        using (ClientContext ctx = TokenHelper.CreateAppEventClientContext(properties, useAppWeb: false))
                        {

                            //Ensure this is set to false
                            SetInstalling(false, ctx);

                            try
                            {
                                //Set list instance details for uninstalling, based on web.config
                                SetListInstanceDetails();

                                
                                AppInstallationHelper.DeleteLists(ctx);
                                AppInstallationHelper.RemoveCustomActions(ctx);
                                
                                result.Status = SPRemoteEventServiceStatus.Continue;

                            }
                            catch (Exception ex)
                            {
                                result.Status = SPRemoteEventServiceStatus.CancelWithError;
                            }
                        }



                        //}


                    }
                    break;
            }



            return result;
        }

        private static void SetListInstanceDetails()
        {
            //Optionall retrieve list title and description settings from web.config
            //Use class default values if not specified
            try
            {
                //Site Collection list title
                string siteCollListTitle = ConfigurationManager.AppSettings["SiteCollectionListTitle"];
                if (!string.IsNullOrEmpty(siteCollListTitle))
                    AppInstallationHelper.SiteCollectionListTitle = siteCollListTitle;

                //Site Collection list description
                string siteCollListDesc = ConfigurationManager.AppSettings["SiteCollectionListDescription"];
                if (!string.IsNullOrEmpty(siteCollListDesc))
                    AppInstallationHelper.SiteCollectionListDescription = siteCollListDesc;

                //Subsite list title
                string subsiteListTitle = ConfigurationManager.AppSettings["SubsiteListTitle"];
                if (!string.IsNullOrEmpty(subsiteListTitle))
                    AppInstallationHelper.SubsiteListTitle = subsiteListTitle;

                //Subsite list description
                string subsiteListDesc = ConfigurationManager.AppSettings["SubsiteListDescription"];
                if (!string.IsNullOrEmpty(subsiteListDesc))
                    AppInstallationHelper.SubsiteListDescription = subsiteListDesc;


                //SiteTemplate list title
                string siteTemplateListTitle = ConfigurationManager.AppSettings["SiteTemplateListTitle"];
                if (!string.IsNullOrEmpty(siteTemplateListTitle))
                    AppInstallationHelper.SiteTemplateListTitle = siteTemplateListTitle;

                //siteTemplate list description
                string siteTemplateListDesc = ConfigurationManager.AppSettings["SiteTemplateListDescription"];
                if (!string.IsNullOrEmpty(siteTemplateListDesc))
                    AppInstallationHelper.SiteTemplateListDescription = siteTemplateListDesc;

            }
            catch (System.Configuration.ConfigurationErrorsException ex)
            {
                //Do nothing; use default values
                Console.Write(String.Format("An exception occurred reading values from the config file: {0}.  Default list titles and descriptions will be used.", ex.Message));
            }
        }

        /// <summary>
        /// This method is a required placeholder, but is not used by app events.
        /// </summary>
        /// <param name="properties">Unused.</param>
        public void ProcessOneWayEvent(SPRemoteEventProperties properties)
        {
            throw new NotImplementedException();
        }

    }
}
