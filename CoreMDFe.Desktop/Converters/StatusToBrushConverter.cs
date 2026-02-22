using Avalonia.Data.Converters;
using Avalonia.Media;
using CoreMDFe.Core.Entities;
using System;
using System.Globalization;

namespace CoreMDFe.Desktop.Converters
{
    public class StatusToBrushConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is StatusManifesto status)
            {
                return status switch
                {
                    StatusManifesto.Autorizado => new SolidColorBrush(Color.Parse("#4CAF50")), // Verde
                    StatusManifesto.Rejeitado => new SolidColorBrush(Color.Parse("#F44336")),  // Vermelho
                    StatusManifesto.Encerrado => new SolidColorBrush(Color.Parse("#2196F3")),  // Azul
                    StatusManifesto.Cancelado => new SolidColorBrush(Color.Parse("#9E9E9E")),  // Cinza
                    _ => new SolidColorBrush(Color.Parse("#FF9800"))                           // Laranja
                };
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}