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
    /// Interaction logic for MainMenu.xaml
    /// </summary>
    public partial class MainMenu : Window
    {
        public MainMenu()
        {
            InitializeComponent();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            InventoryRecord inventory = new InventoryRecord();
            inventory.Show();
            this.Close();
        }
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            CheckIn checkIn = new CheckIn();
            checkIn.Show();
            this.Close();
        }
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            DiagnosisWizard dw = new DiagnosisWizard();
            dw.Show();
            this.Close();
        }
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            MedicalRecord medicalRecord = new MedicalRecord();
            medicalRecord.Show();
            this.Close();
        }
        private void Button_Click_LogOut(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result;
            result = MessageBox.Show(this, "Do you want to exit?", "Log Out", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if(result == MessageBoxResult.Yes)
            {
				Session.getCurrentSession().getCurrentUser().logout();
                MainWindow login = new MainWindow();
                login.Show();
                this.Close();
            }
        }
    }
}
