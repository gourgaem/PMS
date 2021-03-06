﻿using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace PMS.UIManager.Views.ChildWindows
{
    /// <summary>
    /// Interaction logic for AddAccountWindow.xaml
    /// </summary>
    public partial class AddTimeSlotWindow : ChildWindow
    {
		private MySqlConnection conn;
		private DBConnectionManager dbman;
		private PMSUtil pmsutil;

		private TimeSlots _caller;

        public AddTimeSlotWindow(TimeSlots caller)
        {
			_caller = caller;
            InitializeComponent();
			SyncTimePicker();

		}
		private void SyncTimePicker()
		{
			for (int i = 1; i < 13; i++)
			{
				HourPicker.Items.Add(i.ToString("D2"));
			}
			for (int i = 0; i < 61; i++)
			{
				MinutePicker.Items.Add(i.ToString("D2"));
			}
		}
		private void Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			this.Close();
		}
		internal bool CheckDupli()
		{
			dbman = new DBConnectionManager();
			pmsutil = new PMSUtil();
			using (conn = new MySqlConnection(dbman.GetConnStr()))
			{
				conn.Open();
				if (conn.State == ConnectionState.Open)
				{
					string selTime = HourPicker.Text + ":" + MinutePicker.Text + " " + ModePicker.Text;

					MySqlCommand cmd = conn.CreateCommand();
					cmd.CommandText = "SELECT COUNT(*) FROM timeslots WHERE timeslot = @tslot";
					cmd.Prepare();
					cmd.Parameters.AddWithValue("@tslot", DateTime.Parse(selTime).ToString("HH:mm:ss"));
					using (MySqlDataReader db_reader = cmd.ExecuteReader())
					{
						while (db_reader.Read())
						{
							if (db_reader.GetInt32("COUNT(*)") > 0)
							{
								return true;
							}
						}
					}
				}
			}
			return false;
		}
		private void CreateAccountButton_Click(object sender, RoutedEventArgs e)
		{
			if (CheckDupli() == true)
			{
				MsgDupli();
			}
			else {
				dbman = new DBConnectionManager();
				pmsutil = new PMSUtil();
				using (conn = new MySqlConnection(dbman.GetConnStr()))
				{
					conn.Open();
					if (conn.State == ConnectionState.Open)
					{
						string uid = Application.Current.Resources["uid"].ToString();
						string[] dt = pmsutil.GetServerDateTime().Split(null);
						DateTime cDate = Convert.ToDateTime(dt[0]);
						DateTime cTime = DateTime.Parse(dt[1] + " " + dt[2]);
						string curDate = cDate.ToString("yyyy-MM-dd");
						string curTime = cTime.ToString("HH:mm:ss");

						string selTime = HourPicker.Text + ":" + MinutePicker.Text + " " + ModePicker.Text;
						string tid = pmsutil.GenTimeSlotID();
						MySqlCommand cmd = conn.CreateCommand();
						cmd.CommandText =
						"INSERT INTO timeslots(timeslot_id, timeslot, status)" +
						"VALUES(@timeslot_id, @timeslot, @status)";
						cmd.Prepare();
						cmd.Parameters.AddWithValue("@timeslot_id", tid);
						cmd.Parameters.AddWithValue("@timeslot", DateTime.Parse(selTime).ToString("HH:mm:ss"));
						cmd.Parameters.AddWithValue("@status", Status.Text);
						int stat_code = cmd.ExecuteNonQuery();
						conn.Close();
						if (stat_code > 0)
						{
							_caller.SyncTimeSlots();
							pmsutil.LogAccount("Added Timeslot: " + DateTime.Parse(selTime).ToString("HH:mm:ss"));
							MsgSuccess();
							this.Close();
						}
						else
						{
							MsgFail();
						}
					}
					else
					{

					}
				}
			}
		}
		private async void MsgDupli()
		{
			var metroWindow = (Application.Current.MainWindow as MetroWindow);
			await metroWindow.ShowMessageAsync("Failed!", "The timeslot already exists.");
		}
		private async void MsgSuccess()
		{
			var metroWindow = (Application.Current.MainWindow as MetroWindow);
			await metroWindow.ShowMessageAsync("Success!", "The timeslot has been saved successfully.");
		}
		private async void MsgFail()
		{
			var metroWindow = (Application.Current.MainWindow as MetroWindow);
			await metroWindow.ShowMessageAsync("Failed!", "The requested action failed. Please check your input and try again.");
		}
	}
}
