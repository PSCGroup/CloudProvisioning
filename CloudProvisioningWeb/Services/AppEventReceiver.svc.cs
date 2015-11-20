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

                                        var web = ctx.Web;
                                        ctx.Load(web, w => w.Url);
                                        ctx.ExecuteQuery();

                                        //Upload large icons
                                        AppInstallationHelper.UploadFileToLibrary(ctx, "Site Assets", "Icons\\LSiteIcon.png");
                                        AppInstallationHelper.UploadFileToLibrary(ctx, "Site Assets", "Icons\\LSubsiteIcon.png");
                                        AppInstallationHelper.UploadFileToLibrary(ctx, "Site Assets", "Icons\\LTemplatesIcon.png");

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
                                        AppInstallationHelper.AddCustomRibbonAction(ctx, "CustomActions", "CustomActionScript.js", "CustomActionDefinition.xml", "Client Site Collections", "Project Subsites");
                                        

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
