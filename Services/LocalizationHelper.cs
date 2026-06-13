using Microsoft.Extensions.Localization;
using meow.Resources;

namespace meow.Services
{
    public static class LocalizationHelper
    {
        public static string OrderStatus(IStringLocalizer<SharedResources> l, string? status) => status switch
        {
            "W przygotowaniu" => l["Status_Preparing"].Value,
            "Nowe" => l["Status_New"].Value,
            "Wysłana" => l["Status_Shipped"].Value,
            _ => status ?? l["Common_NoData"].Value
        };

        public static string RentalStatus(IStringLocalizer<SharedResources> l, string status) => status switch
        {
            "Rozliczone" => l["RentalStatus_Settled"].Value,
            "Zaległość" => l["RentalStatus_Overdue"].Value,
            "Aktywne" => l["RentalStatus_Active"].Value,
            "Zwrócona" => l["RentalStatus_Returned"].Value,
            "Wypożyczona" => l["RentalStatus_Borrowed"].Value,
            _ => status
        };

        public static string CopyCondition(IStringLocalizer<SharedResources> l, string stan) => stan switch
        {
            "idealny" => l["Cond_Ideal"].Value,
            "bardzo dobry" => l["Cond_VeryGood"].Value,
            "dobry" => l["Cond_Good"].Value,
            "zużyty" => l["Cond_Worn"].Value,
            "zniszczony" => l["Cond_Damaged"].Value,
            _ => stan
        };
    }
}
