using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Menu.Models;
using OrchardCore.Menu.ViewModels;

namespace OrchardCore.Menu.Drivers
{
    public class MenuPartDisplayDriver : ContentPartDisplayDriver<MenuPart>
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;
        protected readonly IHtmlLocalizer H;
        private readonly INotifier _notifier;
        private readonly ILogger _logger;

        public MenuPartDisplayDriver(
            IContentDefinitionManager contentDefinitionManager,
            IHtmlLocalizer<MenuPartDisplayDriver> htmlLocalizer,
            INotifier notifier,
            ILogger<MenuPartDisplayDriver> logger
            )
        {
            _contentDefinitionManager = contentDefinitionManager;
            H = htmlLocalizer;
            _notifier = notifier;
            _logger = logger;
        }

        public override IDisplayResult Edit(MenuPart part)
        {
            return Initialize<MenuPartEditViewModel>("MenuPart_Edit", async model =>
            {
                var menuItemContentTypes = (await _contentDefinitionManager.ListTypeDefinitionsAsync()).Where(t => t.StereotypeEquals("MenuItem"));
                var notify = false;

                foreach (var menuItem in part.ContentItem.As<MenuItemsListPart>().MenuItems)
                {
                    if (!menuItemContentTypes.Any(c => c.Name == menuItem.ContentType))
                    {
                        _logger.LogWarning("The menu item content item with id {ContentItemId} has no matching {ContentType} content type definition.", menuItem.ContentItem.ContentItemId, menuItem.ContentItem.ContentType);
                        await _notifier.WarningAsync(H["The menu item content item with id {0} has no matching {1} content type definition.", menuItem.ContentItem.ContentItemId, menuItem.ContentItem.ContentType]);
                        notify = true;
                    }
                }

                if (notify)
                {
                    await _notifier.WarningAsync(H["Publishing this content item may erase created content. Fix any content type issues beforehand."]);
                }

                model.MenuPart = part;
                model.MenuItemContentTypes = menuItemContentTypes;
            });
        }

        public override async Task<IDisplayResult> UpdateAsync(MenuPart part, IUpdateModel updater)
        {
            var model = new MenuPartEditViewModel();

            if (await updater.TryUpdateModelAsync(model, Prefix, t => t.Hierarchy) && !string.IsNullOrWhiteSpace(model.Hierarchy))
            {
                var originalMenuItems = part.ContentItem.As<MenuItemsListPart>();

                var newHierarchy = JArray.Parse(model.Hierarchy);

                var menuItems = new JsonArray();

                foreach (var item in newHierarchy)
                {
                    menuItems.Add(ProcessItem(originalMenuItems, item as JsonObject));
                }

                part.ContentItem.Content["MenuItemsListPart"] = new JsonObject { ["MenuItems"] = menuItems };
            }

            return Edit(part);
        }

        /// <summary>
        /// Clone the content items at the specific index.
        /// </summary>
        private static JsonObject GetMenuItemAt(MenuItemsListPart menuItems, int[] indexes)
        {
            ContentItem menuItem = null;

            foreach (var index in indexes)
            {
                menuItem = menuItems.MenuItems[index];
                menuItems = menuItem.As<MenuItemsListPart>();
            }

            var newObj = JObject.FromObject(menuItem, JOptions.Default);
            if (newObj["MenuItemsListPart"] != null)
            {
                newObj["MenuItemsListPart"] = new JsonObject { ["MenuItems"] = new JsonArray() };
            }

            return newObj;
        }

        private JsonObject ProcessItem(MenuItemsListPart originalItems, JsonObject item)
        {
            var contentItem = GetMenuItemAt(originalItems, item["index"].ToString().Split('-').Select(x => Convert.ToInt32(x)).ToArray());

            var children = item["children"] as JsonArray;

            if (children is not null)
            {
                var menuItems = new JsonArray();
                for (var i = 0; i < children.Count; i++)
                {
                    menuItems.Add(ProcessItem(originalItems, children[i] as JsonObject));
                    contentItem["MenuItemsListPart"] = new JsonObject { ["MenuItems"] = menuItems };
                };
            }

            return contentItem;
        }
    }
}
