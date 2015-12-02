
//MDS functionality taken from PnP sample on GitHub: https://github.com/OfficeDev/PnP/tree/master/Samples/Core.EmbedJavaScript
//Is MDS enabled?
if ("undefined" != typeof g_MinimalDownload && g_MinimalDownload && (window.location.pathname.toLowerCase()).endsWith("/_layouts/15/start.aspx") && "undefined" != typeof asyncDeltaManager) {
    // Register script for MDS if possible
    RegisterModuleInit("CustomActionScript.js", JavaScript_Embed); //MDS registration
    JavaScript_Embed(); //non MDS run
} else {
    JavaScript_Embed();
}

//Ensure that jQuery is loaded, then call $(document).ready
function JavaScript_Embed() {
    SP.SOD.executeOrDelayUntilScriptLoaded(function () {
        var ctx = SP.ClientContext.get_current();
        var web = ctx.get_web();
        ctx.load(web);
        ctx.executeQueryAsync(function () {
            var url = web.get_url();
            var jQueryLink = url + "/SiteAssets/jquery-1.10.2.min.js";

            loadScript(jQueryLink, function () {
                $(document).ready(function () {
                    updateForm();
                });
            });
        }, function () {
            //Ignore
        });
    }, "SP.js");


}

//Load the JavaScript file with the supplied URL, then call the callback function after the script is loaded
function loadScript(url, callback) {
    var head = document.getElementsByTagName("head")[0];
    var script = document.createElement("script");
    script.src = url;

    // Attach handlers for all browsers
    var done = false;
    script.onload = script.onreadystatechange = function () {
        if (!done && (!this.readyState
            || this.readyState == "loaded"
            || this.readyState == "complete")) {
            done = true;

            // Continue your code
            callback();

            // Handle memory leak in IE
            script.onload = script.onreadystatechange = null;
            head.removeChild(script);
        }
    };

    head.appendChild(script);
}

//Get URL Parameters utility function
function getParameterByName(name) {
    name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
    var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"),
        results = regex.exec(location.search);
    return results === null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
}

//Update the "Provisioning Status" column of the selected item
function UpdateProvisioningStatus(val) {

    var ctx = SP.ClientContext.get_current();
    var web = ctx.get_web();
    ctx.load(web);
    var currentlibid = SP.ListOperation.Selection.getSelectedList();
    var currentLib = web.get_lists().getById(currentlibid);
    var selectedItems = SP.ListOperation.Selection.getSelectedItems(ctx);
    if (selectedItems.length > 0) {

        var sites = "";

        for (var p = 0; p < selectedItems.length; p++) {
            var selectedItem = currentLib.getItemById(selectedItems[p].id);
            ctx.load(selectedItem);
        }
        ctx.executeQueryAsync(function () {
            for (var q = 0; q < selectedItems.length; q++) {
                var selectedItem = currentLib.getItemById(selectedItems[q].id);
                if (selectedItem != null) {
                    sites += selectedItem.get_item("Title") + "\n";
                }
            }

            var c;
            if (val == "Requested") {
                c = confirm("Are you sure you want to request provisioning for the following site?\n\n " + sites);
            }
            else if (val == "Canceled") {
                c = confirm("Are you sure you want to cancel the provisioning request for the following site?\n\n " + sites);
            }
            if (c) {
                for (var s = 0; s < selectedItems.length; s++) {

                    var request = currentLib.getItemById(selectedItems[s].id);
                    ctx.load(request);
                    if (request != null) {

                        request.set_item('ProvisioningStatus', val);
                        request.set_item('ErrorMessage', '');
                        request.update();
                        ctx.executeQueryAsync(Function.createDelegate(this, onUpdateSucceeded), Function.createDelegate(this, onUpdateFailed));

                    }
                }

            }


        }, function () {
            //Ignore
        });





    }

    function onUpdateSucceeded(sender, args) {
        location.reload();
    }

    function onUpdateFailed(sender, args) {
        //alert('Error occured' + args.get_message());
    }

}


//Enable or disable "Request provisioning" button
function EnableRequestProvisioning() {

    var context = SP.ClientContext.get_current();
    var list;
    var selectedItems = SP.ListOperation.Selection.getSelectedItems(context);
    if (selectedItems.length == 1) {
        var web = context.get_web();
        context.load(web);
        var listId = SP.ListOperation.Selection.getSelectedList();
        list = web.get_lists().getById(listId);
        var itemId = selectedItems[0].id;
        if (this.currentItem != null) {
            if (this.itemIdToCheck != null) {
                if (itemId != itemIdToCheck) {
                    GetItemDetails();
                }
            }
        }
        else {
            GetItemDetails();
        }
        return this._canRequest;
    }
    else {
        return false;
    }

    function GetItemDetails() {
        this.currentItem = list.getItemById(selectedItems[0].id);
        context.load(this.currentItem);
        context.executeQueryAsync(Function.createDelegate(this, onRequestQuerySuccess), Function.createDelegate(this, onRequestQueryFailed));
    }


    function onRequestQuerySuccess(sender, args) {
        var status = this.currentItem.get_item('ProvisioningStatus');
        if (status != 'Provisioning...' && status != 'Provisioned' && status != 'Requested') {
            this._canRequest = true;
        }
        else {
            this._canRequest = false;
        }

        this.itemIdToCheck = this.currentItem.get_id();
        RefreshCommandUI();
    }
    function onRequestQueryFailed(sender, args) {
        alert(args.get_message());
    }
}

//Enable or disable "Cancel provisioning" button
function EnableCancelProvisioning() {
    var context_Cancel = SP.ClientContext.get_current();
    var list_Cancel;
    var selectedItems_Cancel = SP.ListOperation.Selection.getSelectedItems(context_Cancel);
    if (selectedItems_Cancel.length == 1) {
        var web_Cancel = context_Cancel.get_web();
        context_Cancel.load(web_Cancel);
        var listId_Cancel = SP.ListOperation.Selection.getSelectedList();
        list_Cancel = web_Cancel.get_lists().getById(listId_Cancel);
        var itemId_Cancel = selectedItems_Cancel[0].id;
        if (this.currentItem_Cancel != null) {
            if (this.itemIdToCheck_Cancel != null) {
                if (itemId_Cancel != itemIdToCheck_Cancel) {
                    GetItemDetails_Cancel();
                }
            }
        }
        else {
            GetItemDetails_Cancel();
        }
        return this._canCancel;
    }
    else {
        return false;
    }

    function GetItemDetails_Cancel() {
        this.currentItem_Cancel = list_Cancel.getItemById(selectedItems_Cancel[0].id);
        context_Cancel.load(this.currentItem_Cancel);
        context_Cancel.executeQueryAsync(Function.createDelegate(this, onCancelQuerySuccess), Function.createDelegate(this, onCancelQueryFailed));
    }

    function onCancelQuerySuccess(sender, args) {
        var status_Cancel = this.currentItem_Cancel.get_item('ProvisioningStatus');
        if (status_Cancel == 'Requested') {
            this._canCancel = true;
        }
        else {
            this._canCancel = false;
        }

        this.itemIdToCheck_Cancel = this.currentItem_Cancel.get_id();
        RefreshCommandUI();
    }
    function onCancelQueryFailed(sender, args) {
        alert(args.get_message());
    }
}

//Enable or disable the "Open site" button
function EnableOpenSite() {
    var context_SiteUrl = SP.ClientContext.get_current();
    var list_SiteUrl;
    var selectedItems_SiteUrl = SP.ListOperation.Selection.getSelectedItems(context_SiteUrl);
    if (selectedItems_SiteUrl.length == 1) {
        var web_SiteUrl = context_SiteUrl.get_web();
        context_SiteUrl.load(web_SiteUrl);
        var listId_SiteUrl = SP.ListOperation.Selection.getSelectedList();
        list_SiteUrl = web_SiteUrl.get_lists().getById(listId_SiteUrl);
        var itemId_SiteUrl = selectedItems_SiteUrl[0].id;
        if (this.currentItem_SiteUrl != null) {
            if (this.itemIdToCheck_SiteUrl != null) {
                if (itemId_SiteUrl != itemIdToCheck_SiteUrl) {
                    GetItemDetails_SiteUrl();
                }
            }
        }
        else {
            GetItemDetails_SiteUrl();
        }
        return this._canOpenSite;
    }
    else {
        return false;
    }

    function GetItemDetails_SiteUrl() {
        this.currentItem_SiteUrl = list_SiteUrl.getItemById(selectedItems_SiteUrl[0].id);
        context_SiteUrl.load(this.currentItem_SiteUrl);
        context_SiteUrl.executeQueryAsync(Function.createDelegate(this, onGetSiteUrlQuerySuccess), Function.createDelegate(this, onGetSiteUrlQueryFailed));
    }

    function onGetSiteUrlQuerySuccess(sender, args) {
        var fieldValue = currentItem_SiteUrl.get_item("LinkToProvisionedSite");
        if (fieldValue != null && fieldValue != "") {
            var url = currentItem_SiteUrl.get_item("LinkToProvisionedSite").get_url();
            if (url != "") {
                this._canOpenSite = true;
            }
            else {
                this._canOpenSite = false;
            }
        }
        else {
            this._canOpenSite = false;
        }

        this.itemIdToCheck_SiteUrl = this.currentItem_SiteUrl.get_id();
        RefreshCommandUI();
    }
    function onGetSiteUrlQueryFailed(sender, args) {
        alert(args.get_message());
    }
}

//Open the site in the "Link to provisioned site" column
function OpenSite() {
    var ctx = SP.ClientContext.get_current();
    var web = ctx.get_web();
    ctx.load(web);
    var currentlibid = SP.ListOperation.Selection.getSelectedList();
    var currentLib = web.get_lists().getById(currentlibid);
    var selectedItems = SP.ListOperation.Selection.getSelectedItems(ctx);
    if (selectedItems.length == 1) {
        var site = currentLib.getItemById(selectedItems[0].id);
        ctx.load(site);
        ctx.executeQueryAsync(Function.createDelegate(this, onUrlQuerySucceeded), Function.createDelegate(this, onUrlQueryFailed));
    }

    function onUrlQuerySucceeded(sender, args) {
        if (site != null) {
            var fieldValue = site.get_item("LinkToProvisionedSite");
            if (fieldValue != null && fieldValue != "") {
                var url = site.get_item("LinkToProvisionedSite").get_url();
                window.open(url);
            }
        }
    }

    function onUrlQueryFailed(sender, args) {
        //alert('Error occured' + args.get_message());
    }

}

//Enable or disable the "New Project Subsite" button 
//DEPRECATED (keeping the code here because it was a pain to write)
function EnableNewSite_Old() {
    var context_NewSubsiteDlg = SP.ClientContext.get_current();
    var list_NewSubsiteDlg;
    var selectedItems_NewSubsiteDlg = SP.ListOperation.Selection.getSelectedItems(context_NewSubsiteDlg);
    if (selectedItems_NewSubsiteDlg.length == 1) {
        var web_NewSubsiteDlg = context_NewSubsiteDlg.get_web();
        context_NewSubsiteDlg.load(web_NewSubsiteDlg);
        var listId_NewSubsiteDlg = SP.ListOperation.Selection.getSelectedList();
        list_NewSubsiteDlg = web_NewSubsiteDlg.get_lists().getById(listId_NewSubsiteDlg);
        var itemId_NewSubsiteDlg = selectedItems_NewSubsiteDlg[0].id;
        if (this.currentItem_NewSubsiteDlg != null) {
            if (this.itemIdToCheck_NewSubsiteDlg != null) {
                if (itemId_NewSubsiteDlg != itemIdToCheck_NewSubsiteDlg) {
                    GetItemDetails_NewSubsiteDlg();
                }
            }
        }
        else {
            GetItemDetails_NewSubsiteDlg();
        }
        return this._canCreateSubsite;
    }
    else {
        return false;
    }

    function GetItemDetails_NewSubsiteDlg() {
        this.currentItem_NewSubsiteDlg = list_NewSubsiteDlg.getItemById(selectedItems_NewSubsiteDlg[0].id);
        context_NewSubsiteDlg.load(this.currentItem_NewSubsiteDlg);
        context_NewSubsiteDlg.executeQueryAsync(Function.createDelegate(this, onGetSiteUrlQuerySuccess), Function.createDelegate(this, onGetSiteUrlQueryFailed));
    }

    function onGetSiteUrlQuerySuccess(sender, args) {
        var provisioningStatus_NewSubsiteDlg = currentItem_NewSubsiteDlg.get_item("ProvisioningStatus");
        if (provisioningStatus_NewSubsiteDlg == "Provisioned") {
            this._canCreateSubsite = true;
        }
        else {
            this._canCreateSubsite = false;
        }

        this.itemIdToCheck_NewSubsiteDlg = this.currentItem_NewSubsiteDlg.get_id();
        RefreshCommandUI();
    }
    function onGetSiteUrlQueryFailed(sender, args) {
        alert(args.get_message());
    }
}

//Enable or disable the "New Project Subsite" button 
function EnableNewSite() {
    var context_NewSubsiteDlg = SP.ClientContext.get_current();
    var list_NewSubsiteDlg;
    var selectedItems_NewSubsiteDlg = SP.ListOperation.Selection.getSelectedItems(context_NewSubsiteDlg);
    if (selectedItems_NewSubsiteDlg.length == 1) {
        return true;
    }
    else {
        return false;
    }
}

//Open the site in the "Link to provisioned site" column
function NewSite() {
    var ctx = SP.ClientContext.get_current();
    var web = ctx.get_web();
    ctx.load(web);
    var currentlibid = SP.ListOperation.Selection.getSelectedList();
    var currentLib = web.get_lists().getById(currentlibid);
    var selectedItems = SP.ListOperation.Selection.getSelectedItems(ctx);
    if (selectedItems.length == 1) {
        var site = currentLib.getItemById(selectedItems[0].id);
        ctx.load(site);
        ctx.executeQueryAsync(Function.createDelegate(this, onUrlQuerySucceeded), Function.createDelegate(this, onUrlQueryFailed));
    }

    function onUrlQuerySucceeded(sender, args) {
        if (site != null) {
            var fieldValue = site.get_item("Title");
            if (fieldValue != null && fieldValue != "") {
                showNewSubsiteDialog(fieldValue, "Project Subsite");
            }
        }
    }

    function onUrlQueryFailed(sender, args) {
        //alert('Error occured' + args.get_message());
    }

}

//Show the "New Subsite" dialog with the parent site collection and site template pre-selected
function showNewSubsiteDialog(parentSiteColl, siteTemplate) {
    SP.SOD.executeOrDelayUntilScriptLoaded(function () {
        var ctx = SP.ClientContext.get_current();
        var web = ctx.get_web();
        ctx.load(web);
        ctx.executeQueryAsync(function () {
            var url = web.get_url();
            var newFormLink = url + "/Lists/Project%20Subsites/NewForm.aspx?SiteCollection=" + parentSiteColl + "&SiteTemplate=" + siteTemplate;
            var dialog = SP.UI.ModalDialog.showModalDialog({
                url: newFormLink,
                title: "New Subsite",
                showClose: true,
                dialogReturnValueCallback: function (result, target) {
                    if (result == SP.UI.DialogResult.OK) {
                        var r = confirm("Your subsite request has been created, but it hasn't been queued for provisioning.  Would you like to go to the Project Subsites list now to request provisioning for this subsite?");
                        if (r) {
                            var listUrl = url + "/Lists/Project%20Subsites";
                            window.open(listUrl);
                        }
                    }
                }

            });


        }, function () {
            //Ignore
        });
    }, "SP.js");



}

//Update the "New Subsite" form by pre-selecting and/or disabling some of the fields, based on query string parameters
function updateForm() {
    //Parent site collection
    var param = getParameterByName('SiteCollection');
    if (param != "" && param != null) {
        var select = $('select[Title="Client Site Required Field"]');
        if (select.length) {
            var option = select.find('option:contains("' + param + '")');
            if (option.length) {
                option.prop("selected", true);
                //Disable-- can't change this
                select.prop("disabled", true);
            }
        }
    }
    //Site template
    var param = getParameterByName('SiteTemplate');
    if (param != "" && param != null) {
        var select = $('select[Title="Site Template Required Field"]');
        if (select.length) {
            var option = select.find('option:contains("' + param + '")');
            if (option.length) {
                //Default to this option, but let user choose
                option.prop("selected", true);

            }
        }
    }

}
