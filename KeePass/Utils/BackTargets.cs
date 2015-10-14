using System;
using System.Linq;
using Microsoft.Phone.Controls;

namespace KeePass.Utils
{
    internal static class BackTargets
    {
        public static void BackToDBs(
            this PhoneApplicationPage page)
        {
            page.BackTo<MainPage>();
        }
        public static void BackToDBPassword(
            this PhoneApplicationPage page)
        {
            if (page.NavigationService.BackStack.Any(_ => _.Source.OriginalString.Contains("Password.xaml")))
                page.BackTo<Password>();
            else page.BackToDBs();
        }


        public static void BackToRoot(
            this PhoneApplicationPage page)
        {
            page.BackTo<GroupDetails>();
        }

        public static void ClearBackStack(
            this PhoneApplicationPage page)
        {
            var service = page.NavigationService;

            while (service.BackStack.Any())
                service.RemoveBackEntry();
        }
    }
}