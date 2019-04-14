﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Middleware;

namespace Interface
{
    /// <summary>
    /// Interaction logic for DiagnosisWizard.xaml
    /// </summary>
    public partial class DiagnosisWizard : Window
    {
        public DiagnosisWizard()
        {
            InitializeComponent();
        }
        private void Button_Click_Previous(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_Next(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_Submit(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_BackMenu(object sender, RoutedEventArgs e)
        {
            MainMenu menu = new MainMenu();
            menu.Show();
            this.Close();
        }

        private void Button_Click_LogOut(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result;
            result = MessageBox.Show(this, "Do you want to exit?", "Log Out", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
				Session.getCurrentSession().getCurrentUser().logout();
				MainWindow login = new MainWindow();
                login.Show();
                this.Close();
            }
        }
       
    }
}
