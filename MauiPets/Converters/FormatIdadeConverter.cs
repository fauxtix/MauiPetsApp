using MauiPets.Resources.Languages;
using System.Globalization;


namespace MauiPets.Converters
{
    public class FormatIdadeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values[0] is int idade)
                {
                    return $"({idade} {AppResources.AgeCaption})";
                }
                return $"({AppResources.AgeCaptionInvalid})";

            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
