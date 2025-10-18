using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FabOS.WebServer.Components.Shared;
using FabOS.WebServer.Services.Implementations;

namespace FabOS.WebServer.Services
{
    public class BreadcrumbService
    {
        private readonly BreadcrumbBuilderService? _breadcrumbBuilderService;

        public event Action? OnBreadcrumbChanged;

        private string _currentBreadcrumb = string.Empty;
        private List<Breadcrumb.BreadcrumbItem> _breadcrumbItems = new List<Breadcrumb.BreadcrumbItem>();

        public string CurrentBreadcrumb
        {
            get => _currentBreadcrumb;
            set
            {
                if (_currentBreadcrumb != value)
                {
                    _currentBreadcrumb = value;
                    OnBreadcrumbChanged?.Invoke();
                }
            }
        }

        public List<Breadcrumb.BreadcrumbItem> BreadcrumbItems => _breadcrumbItems;

        public BreadcrumbService(BreadcrumbBuilderService? breadcrumbBuilderService = null)
        {
            _breadcrumbBuilderService = breadcrumbBuilderService;
        }

        public void SetBreadcrumb(string breadcrumb)
        {
            CurrentBreadcrumb = breadcrumb;
        }

        public void SetBreadcrumbs(params Breadcrumb.BreadcrumbItem[] items)
        {
            _breadcrumbItems = new List<Breadcrumb.BreadcrumbItem>(items);
            OnBreadcrumbChanged?.Invoke();
        }

        /// <summary>
        /// Builds and sets breadcrumbs using the BreadcrumbBuilderService
        /// </summary>
        /// <param name="breadcrumbSpecs">Array of tuples containing (entityType, entityId, url, customLabel)</param>
        public async Task BuildAndSetBreadcrumbsAsync(
            params (string entityType, int? entityId, string? url, string? customLabel)[] breadcrumbSpecs)
        {
            if (_breadcrumbBuilderService == null)
            {
                throw new InvalidOperationException("BreadcrumbBuilderService is not available. Ensure it is registered in DI container.");
            }

            var items = await _breadcrumbBuilderService.BuildBreadcrumbChainAsync(breadcrumbSpecs);
            SetBreadcrumbs(items);
        }

        /// <summary>
        /// Builds and sets a simple two-level breadcrumb (List > Item)
        /// </summary>
        public async Task BuildAndSetSimpleBreadcrumbAsync(
            string listLabel,
            string listUrl,
            string entityType,
            int? entityId = null,
            string? customLabel = null)
        {
            if (_breadcrumbBuilderService == null)
            {
                throw new InvalidOperationException("BreadcrumbBuilderService is not available. Ensure it is registered in DI container.");
            }

            var items = await _breadcrumbBuilderService.BuildSimpleBreadcrumbAsync(
                listLabel, listUrl, entityType, entityId, customLabel);
            SetBreadcrumbs(items);
        }

        public void Clear()
        {
            CurrentBreadcrumb = string.Empty;
            _breadcrumbItems.Clear();
            OnBreadcrumbChanged?.Invoke();
        }
    }
}