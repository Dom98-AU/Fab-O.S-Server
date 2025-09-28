using System;

namespace FabOS.WebServer.Services
{
    public class BreadcrumbService
    {
        public event Action? OnBreadcrumbChanged;

        private string _currentBreadcrumb = string.Empty;

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

        public void SetBreadcrumb(string breadcrumb)
        {
            CurrentBreadcrumb = breadcrumb;
        }

        public void Clear()
        {
            CurrentBreadcrumb = string.Empty;
        }
    }
}