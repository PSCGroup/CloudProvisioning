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
                                    if (!IsInstalling(ctx))
                                    {
                                        SetInstalling(true, ctx);

                                        //Create template library
                                        AppInstallationHelper.CreateTemplateLibrary(ctx);

                                        //Upload sample team sites template
                                        Dictionary<string, string> fieldValues_SampleTemplate = new Dictionary<string, string>();
                                        fieldValues_SampleTemplate.Add("BaseTemplate", "STS#0");
                                        fieldValues_SampleTemplate.Add("SiteDescription", "A sample custom template based on the Team site.");
                                        fieldValues_SampleTemplate.Add("Title", "Sample Team Site");

                                        //Upload PSC client site template
                                        AppInstallationHelper.UploadFileToLibrary(ctx, "Site Templates", "Templates\\SampleTemplate.xml", fieldValues_SampleTemplate);


                                        Dictionary<string, string> fieldValues_PSCClientTemplate = new Dictionary<string, string>();
                                        fieldValues_PSCClientTemplate.Add("BaseTemplate", "BLANKINTERNETCONTAINER#0");
                                        fieldValues_PSCClientTemplate.Add("SiteDescription", "A PSC client site.");
                                        fieldValues_PSCClientTemplate.Add("Title", "PSC Client");

                                        AppInstallationHelper.UploadFileToLibrary(ctx, "Site Templates", "Templates\\PSCClient.xml", fieldValues_PSCClientTemplate);


                                        //Upload PSC project site template
                                        Dictionary<string, string> fieldValues_PSCProjectTemplate = new Dictionary<string, string>();
                                        fieldValues_PSCProjectTemplate.Add("BaseTemplate", "BLANKINTERNETCONTAINER#0");
                                        fieldValues_PSCProjectTemplate.Add("SiteDescription", "A PSC client site.");
                                        fieldValues_PSCProjectTemplate.Add("Title", "PSC Client");

                                        AppInstallationHelper.UploadFileToLibrary(ctx, "Site Templates", "Templates\\PSCProject.xml", fieldValues_PSCProjectTemplate);



                                        //Create client sites list
                                        AppInstallationHelper.CreateClientSiteList(ctx);

                                        //Create project sites list
                                        AppInstallationHelper.CreateProjectSiteList(ctx);

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
                                bool deleteListsOnUninstall = Convert.ToBoolean(ConfigurationManager.AppSettings["DeleteListsOnUninstall"].ToString());

                                if (deleteListsOnUninstall)
                                {
                                    AppInstallationHelper.DeleteLists(ctx);
                                }

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
