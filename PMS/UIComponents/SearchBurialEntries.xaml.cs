﻿using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using MySql.Data.MySqlClient;
using PMS.UIManager.Views.ChildWindows;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;

namespace PMS.UIComponents
{
    /// <summary>
    /// Interaction logic for ConfirmationEntries.xaml
    /// </summary>
    public partial class SearchBurialEntries : UserControl
    {
		//MYSQL
		private MySqlConnection conn;
		private DBConnectionManager dbman;

		private string _query, _type;

		private PMSUtil pmsutil;

		private ObservableCollection<RecordEntryBurial> records;
		private ObservableCollection<RecordEntryBurial> records_final;

		public SearchBurialEntries(string query, string type)
		{
			_query = query;
			_type = type;
			InitializeComponent();
			SyncBurialEntries(query, type);

			ItemsPerPage.SelectionChanged += Update2;
			CurrentPage.ValueChanged += Update;
		}
		private void SyncBurialEntries(string query, string type)
		{
			pmsutil = new PMSUtil();
			records = new ObservableCollection<RecordEntryBurial>();
			records_final = new ObservableCollection<RecordEntryBurial>();

			ComboBoxItem ci = (ComboBoxItem)ItemsPerPage.SelectedItem;
			int itemsPerPage = Convert.ToInt32(ci.Content);
			int page = 1;
			int count = 0;

			dbman = new DBConnectionManager();
			using (conn = new MySqlConnection(dbman.GetConnStr()))
			{
				conn.Open();
				if (conn.State == ConnectionState.Open)
				{
					using (MySqlConnection conn2 = new MySqlConnection(dbman.GetConnStr()))
					{
						conn2.Open();
						MySqlCommand cmd2 = conn2.CreateCommand();
						cmd2.CommandText = "SELECT DISTINCT * FROM records, registers WHERE registers.book_type = @book_type AND records.book_number = registers.book_number AND " +
						"(records.recordholder_fullname LIKE @query OR " +
						"records.parent1_fullname LIKE @query OR " +
						"records.parent2_fullname LIKE @query) ORDER BY records.entry_number ASC;";
						cmd2.Parameters.AddWithValue("@book_type", type);
						cmd2.Parameters.AddWithValue("@query", "%" + query + "%");
						cmd2.Prepare();

						using (MySqlDataReader db_reader2 = cmd2.ExecuteReader())
						{
							string archiveDrive = "init";

							while (db_reader2.Read())
							{
								if (db_reader2.GetString("status") == "Archived")
								{
									string path = @"\archive.db";
									pmsutil = new PMSUtil();
									if (pmsutil.CheckArchiveDrive(path) != "dc")
									{
										archiveDrive = pmsutil.CheckArchiveDrive(path);
										SQLiteConnectionStringBuilder connectionString = new SQLiteConnectionStringBuilder
										{
											FailIfMissing = true,
											DataSource = archiveDrive
										};
										using (SQLiteConnection connection = new SQLiteConnection(connectionString.ToString()))
										{

											// open the connection:
											connection.Open();
											string stm = "SELECT * FROM burial_records WHERE record_id='" + db_reader2.GetString("record_id") + "';";

											using (SQLiteCommand cmdx = new SQLiteCommand(stm, connection))
											{
												using (SQLiteDataReader rdr = cmdx.ExecuteReader())
												{
													while (rdr.Read())
													{
														DateTime dateOfBurial = Convert.ToDateTime(rdr["burial_date"].ToString());
														App.Current.Dispatcher.Invoke((Action)delegate // <--- HERE
														{
															records.Add(new RecordEntryBurial()
															{
																RecordID = db_reader2.GetString("record_id"),
																EntryNumber = db_reader2.GetInt32("entry_number"),
																DeathYear = DateTime.Parse(db_reader2.GetString("record_date")).ToString("yyyy"),
																DeathDate = DateTime.Parse(db_reader2.GetString("record_date")).ToString("MMM dd"),
																BurialYear = dateOfBurial.ToString("yyyy"),
																BurialDate = dateOfBurial.ToString("MMM dd"),
																FullName = db_reader2.GetString("recordholder_fullname"),
																Age = Convert.ToInt32(rdr["age"]),
																Status = rdr["status"].ToString(),
																Residence1 = rdr["residence"].ToString(),
																Residence2 = rdr["residence2"].ToString(),
																Sacrament = rdr["sacrament"].ToString(),
																CauseOfDeath = rdr["cause_of_death"].ToString(),
																PlaceOfInterment = rdr["place_of_interment"].ToString(),
																Parent1 = db_reader2.GetString("parent1_fullname"),
																Parent2 = db_reader2.GetString("parent2_fullname"),
																Stipend = Convert.ToDouble(rdr["stipend"]),
																Minister = rdr["minister"].ToString(),
																Page = page
															});
														});
													}
												}
											}

										}
									}
									else
									{
										archiveDrive = "init";
										App.Current.Dispatcher.Invoke((Action)delegate // <--- HERE
										{
											records.Add(new RecordEntryBurial()
											{
												RecordID = db_reader2.GetString("record_id"),
												EntryNumber = db_reader2.GetInt32("entry_number"),
												DeathYear = DateTime.Parse(db_reader2.GetString("record_date")).ToString("yyyy"),
												DeathDate = DateTime.Parse(db_reader2.GetString("record_date")).ToString("MMM dd"),
												BurialYear = "----",
												BurialDate = "----",
												FullName = db_reader2.GetString("recordholder_fullname"),
												Age = 0,
												Status = "----",
												Residence1 = "----",
												Residence2 = "----",
												Sacrament = "----",
												CauseOfDeath = "----",
												PlaceOfInterment = "----",
												Parent1 = db_reader2.GetString("parent1_fullname"),
												Parent2 = db_reader2.GetString("parent2_fullname"),
												Stipend = 0,
												Minister = "----",
												Page = page
											});
										});
									}
								}
								else
								{
									using (MySqlConnection conn3 = new MySqlConnection(dbman.GetConnStr()))
									{
										conn3.Open();
										MySqlCommand cmd3 = conn3.CreateCommand();
										cmd3.CommandText = "SELECT DISTINCT * FROM burial_records WHERE record_id = @rid;";
										cmd3.Parameters.AddWithValue("@rid", db_reader2.GetString("record_id"));
										cmd3.Prepare();
										using (MySqlDataReader db_reader3 = cmd3.ExecuteReader())
										{
											while (db_reader3.Read())
											{
												App.Current.Dispatcher.Invoke((Action)delegate // <--- HERE
												{
													records.Add(new RecordEntryBurial()
													{
														RecordID = db_reader2.GetString("record_id"),
														EntryNumber = db_reader2.GetInt32("entry_number"),
														DeathYear = DateTime.Parse(db_reader2.GetString("record_date")).ToString("yyyy"),
														DeathDate = DateTime.Parse(db_reader2.GetString("record_date")).ToString("MMM dd"),
														BurialYear = DateTime.Parse(db_reader3.GetString("burial_date")).ToString("yyyy"),
														BurialDate = DateTime.Parse(db_reader3.GetString("burial_date")).ToString("MMM dd"),
														FullName = db_reader2.GetString("recordholder_fullname"),
														Age = db_reader3.GetInt32("age"),
														Status = db_reader3.GetString("status"),
														Residence1 = db_reader3.GetString("residence"),
														Residence2 = db_reader3.GetString("residence2"),
														Sacrament = db_reader3.GetString("sacrament"),
														CauseOfDeath = db_reader3.GetString("cause_of_death"),
														PlaceOfInterment = db_reader3.GetString("place_of_interment"),
														Parent1 = db_reader2.GetString("parent1_fullname"),
														Parent2 = db_reader2.GetString("parent2_fullname"),
														Stipend = db_reader3.GetFloat("stipend"),
														Minister = db_reader3.GetString("minister"),
														Page = page
													});
												});
											}
										}
									}
								}
								count++;
								if (count == itemsPerPage)
								{
									page++;
									count = 0;
								}
							}
						}
					}
					foreach (var cur in records)
					{
						if (cur.Page == CurrentPage.Value)
						{
							records_final.Add(new RecordEntryBurial()
							{
								RecordID = cur.RecordID,
								EntryNumber = cur.EntryNumber,
								DeathYear = cur.DeathYear,
								DeathDate = cur.DeathDate,
								BurialYear = cur.BurialYear,
								BurialDate = cur.BurialDate,
								FullName = cur.FullName,
								Age = cur.Age,
								Status = cur.Status,
								Residence1 = cur.Residence1,
								Residence2 = cur.Residence2,
								Sacrament = cur.Sacrament,
								CauseOfDeath = cur.CauseOfDeath,
								PlaceOfInterment = cur.PlaceOfInterment,
								Parent1 = cur.Parent1,
								Parent2 = cur.Parent2,
								Stipend = cur.Stipend,
								Minister = cur.Minister,
								Page = cur.Page
							});
						}
					}

					SyncChanges();
					System.Threading.Thread.Sleep(1);

					CurrentPage.Maximum = page;
				}
				else
				{

				}
			}
		}
		private async void MsgNoItemSelected()
		{
			var metroWindow = (Application.Current.MainWindow as MetroWindow);
			await metroWindow.ShowMessageAsync("Oops!", "There is no item selected. Please try again.");
		}
		private async void Remarks_Click(object sender, RoutedEventArgs e)
		{
			RecordEntryBurial record = (RecordEntryBurial)EntriesHolder.SelectedItem;
			if (record == null)
			{
				MsgNoItemSelected();
			}
			else
			{
				var metroWindow = (Application.Current.MainWindow as MetroWindow);
				await metroWindow.ShowChildWindowAsync(new ViewRemarksWindow(record.RecordID), this.ParentGrid);
			}
		}

		private async void Print_Click(object sender, RoutedEventArgs e)
		{
			if (EntriesHolder.SelectedItems.Count == 1)
			{
				RecordEntryBurial record = (RecordEntryBurial)EntriesHolder.SelectedItem;

				if (record == null)
				{
					MsgNoItemSelected();
				}
				else
				{
					var metroWindow = (Application.Current.MainWindow as MetroWindow);
					await metroWindow.ShowChildWindowAsync(new PrintBurialRecordEntryWindow(record.RecordID));
				}
			}
			else
			{
				var metroWindow = (Application.Current.MainWindow as MetroWindow);
				await metroWindow.ShowChildWindowAsync(new ConfirmBatchPrintWindow(EntriesHolder.SelectedItems, "Burial"));
			}
		}

		private async void Edit_Click(object sender, RoutedEventArgs e)
		{
			RecordEntryBurial record = (RecordEntryBurial)EntriesHolder.SelectedItem;
			if (record == null)
			{
				MsgNoItemSelected();
			}
			else
			{
				var metroWindow = (Application.Current.MainWindow as MetroWindow);
				await metroWindow.ShowChildWindowAsync(new EditBurialRecordEntryWindow(record.RecordID));
			}
		}

		private async void History_Click(object sender, RoutedEventArgs e)
		{
			RecordEntryBurial record = (RecordEntryBurial)EntriesHolder.SelectedItem;
			if (record == null)
			{
				MsgNoItemSelected();
			}
			else
			{
				var metroWindow = (Application.Current.MainWindow as MetroWindow);
				await metroWindow.ShowChildWindowAsync(new ViewHistoryWindow(record.RecordID));
			}
		}

		private void UpdateContent(object sender, TextChangedEventArgs e)
		{
			BackgroundWorker worker = new BackgroundWorker
			{
				WorkerReportsProgress = true
			};
			worker.DoWork += AddItems;
			worker.ProgressChanged += Worker_ProgressChanged;
			worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
			worker.RunWorkerAsync(10000);
		}
		private void SyncChanges()
		{
			EntriesHolder.Items.Refresh();
			EntriesHolder.ItemsSource = records_final;
			EntriesHolder.Items.Refresh();
		}
		void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			//if (e.UserState != null)
			//EntriesHolder.Items.Add(e.UserState);
		}
		void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			SyncChanges();
		}
		private void Update(object sender, RoutedPropertyChangedEventArgs<double?> e)
		{
			SyncBurialEntries(_query, _type);
		}
		private void Update2(object sender, SelectionChangedEventArgs e)
		{
			SyncBurialEntries(_query, _type);
		}
		private void AddItems(object sender, DoWorkEventArgs e)
		{
			
		}
	}
}
