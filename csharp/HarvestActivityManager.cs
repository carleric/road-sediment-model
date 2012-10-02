using System;
using System.Data;
using System.Data.SqlClient;
using stillwatersci.rsm.data;
using System.Collections;

namespace stillwatersci.rsm.lib
{

	/// <summary>
	/// Description: SqlServer implementation for managing HarvestActivity data
	/// Programmer: Carl Bolstad
	/// Revisions:
	///		Date:	Who:	Description:
	///		
	///	Copyright © 2004 Stillwater Sciences, All Rights Reserved.
	/// </summary>
	public class HarvestActivityManager : DataManager, IDataManager
	{
		private SqlCommand cmdPopulateHarv;
		private SqlCommand cmdSelectByRunHarv;

		private SqlCommand cmdUpdateEnabled;
		private SqlCommand cmdSelectHarvestUnits;
		private SqlDataAdapter daHarvestUnits;

		private ModelRunManager modelRunManager;
		

		public HarvestActivityManager() : base()
		{
		
			this.cmdPopulateHarv = new SqlCommand("dbo.p_rsm_LoadHarvActivity",connRSM);
			this.cmdPopulateHarv.CommandType = System.Data.CommandType.StoredProcedure;
			this.cmdPopulateHarv.Parameters.Add("@runID",System.Data.SqlDbType.Int);
			this.cmdPopulateHarv.Parameters.Add("@projectionid",System.Data.SqlDbType.VarChar,30);
			this.cmdPopulateHarv.Parameters.Add("@lnversionid",System.Data.SqlDbType.Int);

			cmdSelectByRunHarv = new SqlCommand("select * from rsm_harvestactivity where runid = @runid and harvestunitname = @harvestunitname and lnversionid = @lnversionid and enabledharv = 1", connRSM);
			cmdSelectByRunHarv.Parameters.Add("@runid", SqlDbType.Int);
			cmdSelectByRunHarv.Parameters.Add("@lnversionid", SqlDbType.Int);
			cmdSelectByRunHarv.Parameters.Add("@harvestunitname", SqlDbType.VarChar, 30);

			cmdUpdateEnabled = new SqlCommand("update rsm_HarvestActivity set enabledharv = @enabled where harvestunitname = @harvestunitname and runid = @runid and lnversionid = @lnversionid", connRSM);
			cmdUpdateEnabled.Parameters.Add("@runid", SqlDbType.Int);
			cmdUpdateEnabled.Parameters.Add("@lnversionid", SqlDbType.Int);
			cmdUpdateEnabled.Parameters.Add("@harvestunitname", SqlDbType.VarChar, 30);
			cmdUpdateEnabled.Parameters.Add("@enabled", SqlDbType.Int);

			modelRunManager = new ModelRunManager();

			daData = new SqlDataAdapter("select * from rsm_harvestactivity",connRSM);
			cbData= new SqlCommandBuilder(daData);

			cmdSelectHarvestUnits = new SqlCommand("select harvestunitname from rsm_harvestunits where lnversionid = @lnversionid", connRSM);
			cmdSelectHarvestUnits.Parameters.Add("@lnversionid", SqlDbType.Int);
		}

		
		public void UpdateData(dsRSM data)
		{
			data.EnforceConstraints = false;
			daData.Update(data.rsm_HarvestActivity);
			data.AcceptChanges();
		}

		public void PopulateData(int runID)
		{
			try
			{
				this.cmdPopulateHarv.Parameters["@runID"].Value = runID;
				this.cmdPopulateHarv.Parameters["@projectionid"].Value = modelRunManager.GetProjection(runID);
				this.cmdPopulateHarv.Parameters["@lnversionid"].Value = modelRunManager.GetLNVersion(runID);
				this.cmdPopulateHarv.Connection.Open();
				this.cmdPopulateHarv.ExecuteNonQuery();
				this.cmdPopulateHarv.Connection.Close();
			}
			catch(Exception err)
			{
				if(this.cmdPopulateHarv.Connection.State == System.Data.ConnectionState.Open)
				{
					this.cmdPopulateHarv.Connection.Close();
				}
				throw err;
			}
		}
	
		public int GetCount(int runid)
		{
			dsRSM1 = new dsRSM();
			dsRSM1.EnforceConstraints = false;
			daData.Fill(dsRSM1.rsm_HarvestActivity);
			return dsRSM1.rsm_HarvestActivity.Select("runid = " + runid).Length;
		}

		public void FillDataForRunHarv(int runID, string HarvestUnitName, dsRSM data)
		{
			data.rsm_HarvestActivity.Clear();
			daData.SelectCommand = cmdSelectByRunHarv;
			cmdSelectByRunHarv.Parameters["@runid"].Value = runID;
			cmdSelectByRunHarv.Parameters["@lnversionid"].Value = modelRunManager.GetLNVersion(runID);
			cmdSelectByRunHarv.Parameters["@harvestunitname"].Value = HarvestUnitName;
			
			data.EnforceConstraints = false;
			daData.Fill(data.rsm_HarvestActivity);
		}

		public void UpdateEnabled(int runid, string harvestunitname, int enabled)
		{
			try
			{
				cmdUpdateEnabled.Parameters["@runid"].Value = runid;
				cmdUpdateEnabled.Parameters["@lnversionid"].Value = modelRunManager.GetLNVersion(runid);
				cmdUpdateEnabled.Parameters["@harvestunitname"].Value = harvestunitname;
				cmdUpdateEnabled.Parameters["@enabled"].Value = enabled;

				connRSM.Open();
				cmdUpdateEnabled.ExecuteNonQuery();
			}
			catch(Exception err)
			{
				throw err;
			}
			finally
			{
				connRSM.Close();
			}

		}

		
		public string [] GetHarvestUnits(int lnversionid)
		{
			ArrayList harvs = new ArrayList();
			try
			{
				cmdSelectHarvestUnits.Parameters["@lnversionid"].Value = lnversionid;
				connRSM.Open();
				SqlDataReader dr = cmdSelectHarvestUnits.ExecuteReader();
				while(dr.Read())
				{
					harvs.Add(dr.GetValue(0));
				}
				connRSM.Close();
			}
			catch(Exception err)
			{
				throw err;
			}
			finally
			{
				if(connRSM.State == ConnectionState.Open) connRSM.Close();
			}
			return harvs.ToArray(Type.GetType("System.String")) as string [];

		}

		
	}

} 
