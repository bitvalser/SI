﻿using SICore;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace SIGame.Behaviors
{
    public static class ImageLoader
    {
        public static PersonAccount GetImageSource(DependencyObject obj)
        {
            return (PersonAccount)obj.GetValue(ImageSourceProperty);
        }

        public static void SetImageSource(DependencyObject obj, PersonAccount value)
        {
            obj.SetValue(ImageSourceProperty, value);
        }

        // Using a DependencyProperty as the backing store for ImageSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.RegisterAttached("ImageSource", typeof(PersonAccount), typeof(ImageLoader), new PropertyMetadata(null, OnImageSourceChanged));

        private static void OnImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var image = (Image)d;

            var person = (PersonAccount)e.NewValue;

            void handler(object s, PropertyChangedEventArgs e2)
            {
                if (e2.PropertyName == nameof(PersonAccount.Picture) || e2.PropertyName == nameof(PersonAccount.IsConnected))
                {
                    UpdateAvatar(person, image);
                }
            }

            if (e.OldValue != null)
            {
                ((PersonAccount)e.OldValue).PropertyChanged -= handler;
            }

            if (person == null)
            {
                if (e.OldValue != null)
                {
                    image.Source = null;
                }
            }
            else
            {
                person.PropertyChanged += handler;
                UpdateAvatar(person, image);
            }
        }

        private static async void UpdateAvatar(PersonAccount person, Image image)
        {
            if (image.Dispatcher != System.Windows.Threading.Dispatcher.CurrentDispatcher)
            {
                await image.Dispatcher.InvokeAsync(() => UpdateAvatar(person, image));
                return;
            }

            if (!person.IsConnected)
            {
                image.Source = null;
                return;
            }

            var value = person.Picture;

            var path = value?.ToString();

            if (string.IsNullOrWhiteSpace(path))
            {
                var avatarCode = person.IsMale ? "m" : "f";

                var defaultImage = new BitmapImage();
                defaultImage.BeginInit();
                defaultImage.UriSource = new Uri($"pack://application:,,,/SIGame;component/Resources/avatar-{avatarCode}.png");
                defaultImage.EndInit();

                image.Source = defaultImage;
                return;
            }

            if (!Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out Uri uri))
            {
                return;
            }

            if (!uri.IsAbsoluteUri)
            {
                return;
            }

            var isLocalFile = uri.Scheme == "file";
            if (isLocalFile && !File.Exists(uri.LocalPath))
            {
                return;
            }

            if (isLocalFile)
            {
                try
                {
                    var decoder = BitmapDecoder.Create(uri, BitmapCreateOptions.None, BitmapCacheOption.OnLoad); // Лучше эти два параметра не трогать, так как в противном случае в некоторых ситуациях изображения могут перестать отображаться
                    if (decoder.Frames.Count == 0)
                    {
                        return;
                    }

                    var frame = decoder.Frames[0];
                    image.Source = frame.CanFreeze ? (BitmapSource)frame.GetAsFrozen() : frame;
                }
                catch
                {

                }

                return;
            }

            try
            {
                var stream = await Task.Run(() => LoadImage(uri));

                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.Default); // Лучше эти два параметра не трогать, так как в противном случае в некоторых ситуациях изображения могут перестать отображаться
                if (decoder.Frames.Count == 0)
                {
                    return;
                }

                var frame = decoder.Frames[0];
                image.Source = frame.CanFreeze ? (BitmapSource)frame.GetAsFrozen() : frame;
            }
            catch
            {

            }
        }

        /// <summary>
        /// Очень важно, что этот код находится в отдельном методе - GetResponseAsync работает не совсем асинхронно
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private static async Task<Stream> LoadImage(Uri uri)
        {
            var request = WebRequest.CreateHttp(uri);
            var response = await request.GetResponseAsync();

            return response.GetResponseStream();
        }
    }
}
