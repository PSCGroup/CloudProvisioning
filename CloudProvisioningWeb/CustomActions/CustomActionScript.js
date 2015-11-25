
//Update the "Provisioning Status" column of the selected item
function UpdateProvisioningStatus(val) {
    var ctx = SP.ClientContext.get_current();
    var web = ctx.get_web();
    ctx.load(web);
    var currentlibid = SP.ListOperation.Selection.getSelectedList();
    var currentLib = web.get_lists().getById(currentlibid);
    var selectedItems = SP.ListOperation.Selection.getSelectedItems(ctx);
    if (selectedItems.length == 1) {
        var request = currentLib.getItemById(selectedItems[0].id);
        ctx.load(request);
        if (request != null) {

            var c;
            if (val == "Requested") {
                c = confirm("Are you sure you want to request provisioning of this site?");
            }
            else if (val == "Canceled") {
                c = confirm("Are you sure you want to cancel the provisioning request for this site?");
            }
            if (c) {
                request.set_item('ProvisioningStatus', val);
                request.set_item('ErrorMessage', '');
                request.update();
                ctx.executeQueryAsync(Function.createDelegate(this, onUpdateSucceeded), Function.createDelegate(this, onUpdateFailed));
            }
        }

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