using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Collections;
using stillwatersci.rsm.data;
using System.Diagnostics;

namespace stillwatersci.rsm.lib
{

	/// <summary>
	/// Description: SqlServer implementation for managing Rainfall data
	/// Programmer: Carl Bolstad
	/// Revisions:
	///		Date:	Who:	Description:
	///		
	///	Copyright © 2004 Stillwater Sciences, All Rights Reserved.
	/// </summary>
	public class RainfallManager : DataManager
	{
		//dataadapters for rsm_Rainfall, rsm_rainfallperiod and rsm_projectedrainfall
		private SqlDataAdapter daMonitoringPoints;
		private SqlCommand cmdSelectRainSelector;
		private SqlCommand cmdSelectRainRecords;
		private SqlCommand cmdSelectRainRecord;


		public RainfallManager() : base()
		{
			daData = new SqlDataAdapter("select * from rsm_rainfall", connRSM);
			SqlCommandBuilder cb1 = new SqlCommandBuilder(daData);

			daMonitoringPoints = new SqlDataAdapter("select * from rsm_monitoringpoints", connRSM);

			cmdSelectRainRecords = new SqlCommand("select rainrecordname from rsm_rainfall group by rainrecordname", connRSM);
			cmdSelectRainRecord = new SqlCommand("select * from rsm_rainfall where rainrecordname = @rainrecordname", connRSM);
			cmdSelectRainRecord.Parameters.Add("@rainrecordname", SqlDbType.VarChar, 30);

		}


		public void ImportRainfall(string inFile, string rainrecordname, string monitoringpointid, int lnversionid)
		{	
			DateTime [] d = GetTipTimes(inFile);
			DateTime [] RainTime;
			double [] RainIntensity;

			//call bindata to translate to 5 min
			BinData binData = new BinData(5,0.254);
			binData.Compute(d, out RainTime, out RainIntensity);

			//call RunoffManager to get runoff
			RunoffManager runoffManager = new RunoffManager();
			runoffManager.InitGammaDistribution(1,40);
			double [] discharge = runoffManager.FillDischarge(RainIntensity, 0.0146, 1, 0.0429);

			//load into rsm_rainfall
			dsRSM1.EnforceConstraints = false;
			daData.Fill(dsRSM1.rsm_Rainfall);
			for(int i = 0; i < RainTime.Length; i++)
			{
				dsRSM.rsm_RainfallRow rainfall = dsRSM1.rsm_Rainfall.Newrsm_RainfallRow();
				rainfall.BeginEdit();
				rainfall.MonitoringPointID = monitoringpointid;
				rainfall.LNVersionID = lnversionid;
				rainfall.RainDateTime = RainTime[i];
				rainfall.RainIntensity = RainIntensity[i];
				rainfall.Runoff = discharge[i];
				rainfall.RainRecordName = rainrecordname;
				rainfall.EndEdit();
				dsRSM1.rsm_Rainfall.Addrsm_RainfallRow(rainfall);
			}
			int wrong = dsRSM1.rsm_Rainfall.Select("RainDateTime = '1/1/1'").Length;
			if(wrong > 0)
			{
				Debug.WriteLine("dates like 1/1/1: " + wrong);
				throw new Exception("date error");
			}
			if(dsRSM1.rsm_Rainfall.HasErrors)
			{
				foreach(DataRow r in dsRSM1.rsm_Rainfall.GetErrors())
				{
					Debug.WriteLine(r.RowError);
					r.RejectChanges();
				}
			}
			daData.Update(dsRSM1.rsm_Rainfall);
			dsRSM1.rsm_Rainfall.AcceptChanges();
		
		}

		private bool DateIsValid(string line)
		{
			try
			{
				DateTime d = DateTime.Parse(line);
				if(d.Year < 1800 || d.Year > 3000 || d.Year == 1)
					return false;

			}
			catch(Exception err)
			{
				return false;
			}
			return true;
		}

		private bool DecIsValid(double d)
		{
			try
			{
				decimal.Parse(d.ToString());
			}
			catch(Exception err)
			{
				return false;
			}
			return true;
		}
		

		public void FillRainSelector(dsRainSelection data)
		{

		}

		public DateTime [] GetTipTimes(string inFile)
		{
			ArrayList tipTimes = new ArrayList();
			//open file
		
			if (!File.Exists(inFile)) 
			{
				throw new Exception("file doesn't exist");
			}

			// Open the file to read from.for each line, add tip time to an array
			using (StreamReader sr = File.OpenText(inFile)) 
			{
				string s = "";
				while ((s = sr.ReadLine()) != null) 
				{
					if(DateIsValid(s))
					{
						tipTimes.Add(DateTime.Parse(s));
					}
					else
						Debug.WriteLine(s);
				}
			}
			
			return tipTimes.ToArray(Type.GetType("System.DateTime")) as DateTime [];
		}
		
	}

} 
