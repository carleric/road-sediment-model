using System;
using System.Data;
using System.Data.SqlClient;
using stillwatersci.rsm.data;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geometry;
using System.Collections;
using System.Runtime.InteropServices;


namespace stillwatersci.rsm.lib
{

	/// <summary>
	/// Description: SqlServer implementation for managing DeliveryPoint data
	/// Programmer: Carl Bolstad
	/// Revisions:
	///		Date:	Who:	Description:
	///		
	///	Copyright © 2004 Stillwater Sciences, All Rights Reserved.
	/// </summary>
	public class DeliveryPointManager : DataManager
	{

		private SqlCommand cmdGetDeliveryPointName;
		private SqlDataAdapter daDeliveryPoints;
		private SqlCommandBuilder cbDelPts;
		private SqlCommand cmdSetMonPt;
		private SqlCommand cmdSetGated;
		private SqlCommand cmdUpdateDelPtHydro;
		private SqlCommand cmdUpdateDelPtLitho;
		private SqlCommand cmdSelectDelPtsNoLitho;
		private SqlCommand cmdUpdateDelPtSingleLitho;

		private IFeatureWorkspace pFeatureworkspace;
		private IFeatureClass MonitoringPoints;
		private IFeatureClass DeliveryPoints;
		private IFeatureClass Roads;
		private IFeatureClass AdminRegion;
		private IFeatureClass Hydro;
		private IFeatureClass LithoTopoUnits;
		



		public DeliveryPointManager()
		{
			cmdGetDeliveryPointName = new SqlCommand("select deliverypointname from rsm_deliverypoints where deliverypointid = @deliverypointid", connRSM);
			cmdGetDeliveryPointName.Parameters.Add("@deliverypointid",SqlDbType.Int);
			
			cmdSetMonPt = new SqlCommand("update rsm_deliverypoints set monitoringpointid = @monitoringpointid where deliverypointid = @deliverypointid and lnversionid = @lnversionid", connRSM);
			cmdSetMonPt.Parameters.Add("@monitoringpointid", SqlDbType.Int);
			cmdSetMonPt.Parameters.Add("@deliverypointid", SqlDbType.Int);
			cmdSetMonPt.Parameters.Add("@lnversionid", SqlDbType.Int);

			cmdSetGated = new SqlCommand("update rsm_deliverypoints set Gated = @gated where deliverypointid = @deliverypointid and lnversionid = @lnversionid", connRSM);
			cmdSetGated.Parameters.Add("@gated", SqlDbType.Bit);
			cmdSetGated.Parameters.Add("@deliverypointid", SqlDbType.Int);
			cmdSetGated.Parameters.Add("@lnversionid", SqlDbType.Int);

			cmdUpdateDelPtHydro = new SqlCommand("p_rsm_UpdateDelPtHydro", connRSM);
			cmdUpdateDelPtHydro.CommandType = CommandType.StoredProcedure;

			cmdUpdateDelPtLitho = new SqlCommand("p_rsm_UpdateDelPtLitho", connRSM);
			cmdUpdateDelPtLitho.Parameters.Add("@lnversionid", SqlDbType.Int);
			cmdUpdateDelPtLitho.CommandType = CommandType.StoredProcedure;

			cmdSelectDelPtsNoLitho = new SqlCommand("select deliverypointid from rsm_deliverypoints where lithocode is null and lnversionid = @lnversionid", connRSM);
			cmdSelectDelPtsNoLitho.Parameters.Add("@lnversionid", SqlDbType.Int);

			cmdUpdateDelPtSingleLitho = new SqlCommand("update rsm_deliverypoints set lithocode = @litho where deliverypointid = @deliverypointid and lnversionid = @lnversionid", connRSM);
			cmdUpdateDelPtSingleLitho.Parameters.Add("@litho", SqlDbType.Char, 10);
			cmdUpdateDelPtSingleLitho.Parameters.Add("@deliverypointid", SqlDbType.Int);
			cmdUpdateDelPtSingleLitho.Parameters.Add("@lnversionid", SqlDbType.Int);

			daDeliveryPoints = new SqlDataAdapter("select * from rsm_deliverypoints", connRSM);
			cbDelPts = new SqlCommandBuilder(daDeliveryPoints);
		}

		
		private void InitSpatial(int lnversionid)
		{
			pFeatureworkspace = Utility.GetWorkspaceForVersion(lnversionid) as IFeatureWorkspace;//GlobalSettings.UserWorkspace as IFeatureWorkspace;
			MonitoringPoints = pFeatureworkspace.OpenFeatureClass(GlobalSettings.Settings.MonitoringPointFeatureClassName);
			DeliveryPoints = pFeatureworkspace.OpenFeatureClass(GlobalSettings.Settings.DeliveryPointFeatureClassName);
			Roads = pFeatureworkspace.OpenFeatureClass(GlobalSettings.Settings.RoadFeatureClassName);
			AdminRegion = pFeatureworkspace.OpenFeatureClass(GlobalSettings.Settings.AdminRegionFeatureClassName);
			Hydro = pFeatureworkspace.OpenFeatureClass(GlobalSettings.Settings.HydroFeatureClassName);
			LithoTopoUnits = pFeatureworkspace.OpenFeatureClass(GlobalSettings.Settings.LithoTopoUnitFeatureClassName);
		
		}



		public void UpdateRoadID(int lnversionid)
		{
			//select monitoring points with f-type as 'RAIN'
			try
			{
				//init spatial
				InitSpatial(lnversionid);

				//get dataset of deliverypoints
				dsRSM1.EnforceConstraints = false;
				dsRSM1.Clear();
				daDeliveryPoints.Fill(dsRSM1.rsm_DeliveryPoints);

				//begin edit on rsm_deliverypoints
				
				//for each deliverypoint feature
				IFeatureCursor delPts = DeliveryPoints.Search(null, false);
				IFeature delPt = delPts.NextFeature();
				while(delPt != null)
				{
					//use search to find nearest road
					IEnumIDs roads = Utility.SearchForFeatures(delPt.Shape, 20, 100, this.Roads, null, null);
					int road = roads.Next();
					ArrayList roadsArray = new ArrayList();
					while(road > 0)
					{
						roadsArray.Add(road);
						road = roads.Next();
					}
					IFeature closest;
					//if more than one, get the closest
					if(roadsArray.Count > 1)
					{
						IPoint pt = delPt.Shape as IPoint;
						closest = Utility.FindClosestFeatureToPoint(pt, Roads, roads);
					}
					//else, use the one
					else
					{
						closest = Roads.GetFeature(int.Parse(roadsArray[0].ToString()));
					}
					
					//get row in rsm_deliverypoints (dataset)	
					dsRSM.rsm_DeliveryPointsRow delpt = 
						dsRSM1.rsm_DeliveryPoints.FindByDeliveryPointIDLNVersionID(delPt.OID,lnversionid);

					delpt.BeginEdit();
					delpt.RoadID = closest.OID; //int.Parse(closest.OID.get_Value(closest.Fields.FindField("RoadID")).ToString());
					delpt.EndEdit();
					delPt = delPts.NextFeature();
				}

				//update rsm_deliveryoints with changes
				daDeliveryPoints.Update(dsRSM1.rsm_DeliveryPoints);
				dsRSM1.rsm_DeliveryPoints.AcceptChanges();
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

		public void UpdateHydroSegID(int lnversionid)
		{
			//select monitoring points with f-type as 'RAIN'
			try
			{

				//init spatial
				InitSpatial(lnversionid);

				//get dataset of deliverypoints
				dsRSM1.EnforceConstraints = false;
				dsRSM1.Clear();
				daDeliveryPoints.Fill(dsRSM1.rsm_DeliveryPoints);

				//begin edit on rsm_deliverypoints
				
				//for each deliverypoint feature
				IFeatureCursor delPts = DeliveryPoints.Search(null, false);
				IFeature delPt = delPts.NextFeature();
				while(delPt != null)
				{
					//use search to find nearest road
					IEnumIDs hydros = Utility.SearchForFeatures(delPt.Shape, 20, 100, this.Hydro, null, null);
					int hydro = hydros.Next();
					ArrayList hydrosArray = new ArrayList();
					while(hydro > 0)
					{
						hydrosArray.Add(hydro);
						hydro = hydros.Next();
					}
					IFeature closest;
					//if more than one, get the closest
					if(hydrosArray.Count > 1)
					{
						IPoint pt = delPt.Shape as IPoint;
						closest = Utility.FindClosestFeatureToPoint(pt, this.Hydro, hydros);
					}
						//else, use the one
					else
					{
						closest = Hydro.GetFeature(int.Parse(hydrosArray[0].ToString()));
					}
					
					//get row in rsm_deliverypoints (dataset)	
					dsRSM.rsm_DeliveryPointsRow delpt = 
						dsRSM1.rsm_DeliveryPoints.FindByDeliveryPointIDLNVersionID(delPt.OID,lnversionid);

					delpt.BeginEdit();
					delpt.HydroSegID = closest.get_Value(closest.Fields.FindField("ExternalID")).ToString();
					delpt.EndEdit();
					delPt = delPts.NextFeature();
				}

				//update rsm_deliveryoints with changes
				daDeliveryPoints.Update(dsRSM1.rsm_DeliveryPoints);
				dsRSM1.rsm_DeliveryPoints.AcceptChanges();

				//overwrite overlay values with those from rmap where valid
				connRSM.Open();
				cmdUpdateDelPtHydro.ExecuteNonQuery();
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

		public void UpdateMonitoringPoint(int lnversionid)
		{
			//select monitoring points with f-type as 'RAIN'
			try
			{
				//init spatial
				InitSpatial(lnversionid);

				IQueryFilter RainFilter = new QueryFilterClass();
				RainFilter.WhereClause = "FTYPE = 'RAIN'";
				
				ISelectionSet RainPoints = this.MonitoringPoints.Select(RainFilter,esriSelectionType.esriSelectionTypeIDSet,esriSelectionOption.esriSelectionOptionNormal,
					(IWorkspace)pFeatureworkspace);
				
				IFeature delPt = null;
				int countDelPts = ((ITable)DeliveryPoints).RowCount(null);
				for(int i = 1; i <= countDelPts; i++)
				{
					delPt = DeliveryPoints.GetFeature(i);
					IPoint pt = delPt.Shape as IPoint;
					IFeature closest = Utility.FindClosestFeatureToPoint(pt, this.MonitoringPoints, RainPoints.IDs);
				
					if(closest != null)
					{
						cmdSetMonPt.Parameters["@deliverypointid"].Value = delPt.OID;
						cmdSetMonPt.Parameters["@monitoringpointid"].Value = closest.OID;
						cmdSetMonPt.Parameters["@lnversionid"].Value = lnversionid;
						cmdSetMonPt.Connection.Open();
						cmdSetMonPt.ExecuteNonQuery();
						cmdSetMonPt.Connection.Close();
					}
				}
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

		public void UpdateGated(int lnversionid)
		{
			
			try
			{
				//init spatial
				InitSpatial(lnversionid);

				//for each deliverypoint feature
				IFeatureCursor delPts = DeliveryPoints.Search(null, false);
				IFeature delPt = delPts.NextFeature();
				while(delPt != null)
				{	
					//loop through all admin regions and test to see if deliverypoint lies inside one
					IQueryFilter query = new QueryFilterClass();
					query.WhereClause = "adminregiontype = 2";
					IFeatureCursor adminregions = AdminRegion.Search(query, false);
					IFeature adminregion = adminregions.NextFeature();
					bool within = false;
					IRelationalOperator relOp;
					while(adminregion != null)
					{
						relOp = delPt.Shape as IRelationalOperator;
						if(relOp.Within(adminregion.Shape))
						{
							within = true;
						}
						adminregion = adminregions.NextFeature();
					}

					Marshal.ReleaseComObject(adminregions);

					cmdSetGated.Parameters["@gated"].Value = within;
					cmdSetGated.Parameters["@deliverypointid"].Value = delPt.OID;
					cmdSetGated.Parameters["@lnversionid"].Value = lnversionid;
					cmdSetGated.Connection.Open();
					cmdSetGated.ExecuteNonQuery();
					cmdSetGated.Connection.Close();
					delPt = delPts.NextFeature();
				}
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

		

		public string GetDeliveryPointName(int deliverypointid)
		{
			string deliverypointname = null;
			try
			{
				cmdGetDeliveryPointName.Parameters["@deliverypointid"].Value = deliverypointid;
				cmdGetDeliveryPointName.Connection.Open();
				deliverypointname = cmdGetDeliveryPointName.ExecuteScalar() as string;
				cmdGetDeliveryPointName.Connection.Close();
			}
			catch(Exception err)
			{
				if(cmdGetDeliveryPointName.Connection.State == System.Data.ConnectionState.Open){cmdGetDeliveryPointName.Connection.Close();}
				throw err;
			}
			return deliverypointname;
		}

	
		public void UpdateLithoCode(int lnversionid)
		{
			try
			{
				//init spatial
				InitSpatial(lnversionid);

				//first use the lithocode of the channelclass where it is known
				cmdUpdateDelPtLitho.Parameters["@lnversionid"].Value = lnversionid;
				connRSM.Open();
				cmdUpdateDelPtLitho.ExecuteNonQuery();
				connRSM.Close();

				//then do an overlay to determine what ltu this deliverypoint is in

				//get the deliverypoints with no lithocode
				int deliverypoint;
				IFeature delPt; 
				IQueryFilter filter = new QueryFilterClass();
				
				this.dsRSM1.EnforceConstraints = false;
				this.daDeliveryPoints.Fill(this.dsRSM1.rsm_DeliveryPoints);
				DataRow [] delpts = dsRSM1.rsm_DeliveryPoints.Select("lnversionid = " + lnversionid + " and lithocode is null");
				foreach(dsRSM.rsm_DeliveryPointsRow delpt in delpts)
				{
					deliverypoint = delpt.DeliveryPointID;//int.Parse(dr.GetValue(0).ToString());
					filter.WhereClause = "objectid = " + deliverypoint;
					IFeatureCursor cursor = DeliveryPoints.Search(filter, true);
					IFeature lithopoly = null;
					delPt = cursor.NextFeature();
					Marshal.ReleaseComObject(cursor);
					if(delPt != null)
					{
						lithopoly = Utility.FindPolyContainingGeometry(delPt.Shape, this.LithoTopoUnits);
						if(lithopoly != null)
						{
							int fieldid = lithopoly.Fields.FindField("lithocode");
							string lithocode = lithopoly.get_Value(fieldid).ToString();

							delpt.LithoCode = lithocode;

						}
					}
				}
				daDeliveryPoints.Update(dsRSM1.rsm_DeliveryPoints);
				dsRSM1.rsm_DeliveryPoints.AcceptChanges();

			}
			catch(Exception err)
			{
				throw err;
			}
			finally
			{
				if(connRSM.State == ConnectionState.Open) connRSM.Close();
			}
		}
		
	}

} 
