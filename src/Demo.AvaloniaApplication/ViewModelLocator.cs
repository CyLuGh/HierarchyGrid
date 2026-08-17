using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Demo.AvaloniaApplication.ViewModels;
using Splat;

namespace Demo.AvaloniaApplication
{
    public static class ViewModelLocator
    {
        static ViewModelLocator()
        {
            SplatRegistrations.RegisterLazySingleton<MainViewModel>();
            var exceptionHandler = new GeneralExceptionHandler();
            SplatRegistrations.RegisterConstant(exceptionHandler);

            SplatRegistrations.SetupIOC();
        }

        public static MainViewModel MainViewModel => Locator.Current.GetService<MainViewModel>()!;
        public static GeneralExceptionHandler GeneralExceptionHandler =>
            Locator.Current.GetService<GeneralExceptionHandler>()!;
    }
}
