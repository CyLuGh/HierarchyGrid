using Demo.AvaloniaApplication.ViewModels;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.AvaloniaApplication
{
    public static class ViewModelLocator
    {
        static ViewModelLocator()
        {
            SplatRegistrations.RegisterLazySingleton<MainViewModel>();

            SplatRegistrations.SetupIOC();
        }
        public static MainViewModel MainViewModel => Locator.Current.GetService<MainViewModel>()!;
    }
}
