using System;
using System.Data;
using System.Data.SqlClient;
using stillwatersci.rsm.data;
using System.Collections;
using System.Diagnostics;

using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.NetworkAnalysis;

using System.Threading;

namespace stillwatersci.rsm.lib
{


	/// <summary>
	/// Description: SqlServer implementation for managing HaulRoute data.
	/// Also contains GIS functions for creating and editing routes.
	/// Programmer: Carl Bolstad
	/// Revisions:
	///		Date:	Who:	Description:
	///		
	///	Copyright © 2004 Stillwater Sciences, All Rights Reserved.
	/// </summary>
	public class HaulRouteManager : DataManager, IDataManager, IHaulRouteManager
	{
		//esri
		private IFeatureWorkspace pFeatureworkspace;
		private IFeatureClass HarvestUnits;
		private IFeatureClass RoadEdge;
		private IFeatureClass RMAPsites;

		//data
		private SqlDataAdapter daHaulRoadIDs;
		private SqlCommandBuilder cbHaulRoadIDs;
		private SqlDataAdapter daRoads;
		private SqlCommand cmdGetHaulName;
		private SqlDataAdapter daGetHaulRoutesForUnit;
		

		private ArrayList harvestUnits;
		private ArrayList destinations;

		private HarvestActivityManager harvestActivityManager;

		

		//network
		private INetwork roadNet;
		private INetElements RoadNetElements;

		public HaulRouteManager() : base()
		{
			
			//haulroute dataadapter and commands
			harvestActivityManager = new HarvestActivityManager();
			
			daData = new SqlDataAdapter("select * from rsm_harvesthaulroutes", connRSM);
			cbData = new SqlCommandBuilder(daData);
			daHaulRoadIDs = new SqlDataAdapter("select * from rsm_HaulRoadIDs", connRSM);
			cbHaulRoadIDs = new SqlCommandBuilder(daHaulRoadIDs);
			daRoads = new SqlDataAdapter("select * from rsm_Roads", connRSM);
					
			//arrays
			harvestUnits = new ArrayList();
			destinations = new ArrayList();

			//queries
			cmdGetHaulName = new SqlCommand("select haulroutename from rsm_harvesthaulroutes where haulid = @haulid", connRSM);
			cmdGetHaulName.Parameters.Add("@haulid",SqlDbType.Int);

			daGetHaulRoutesForUnit = new SqlDataAdapter("select * from rsm_harvesthaulroutes where harvestunitname = @harvestunitname and lnversionid = @lnversionid", connRSM);
			daGetHaulRoutesForUnit.SelectCommand.Parameters.Add("@harvestunitname", SqlDbType.VarChar, 10);
			daGetHaulRoutesForUnit.SelectCommand.Parameters.Add("@lnversionid", SqlDbType.Int);

			
			
		}

		public void FillData(dsRSM data)
		{
			data.EnforceConstraints = false;
			daData.Fill(data.rsm_HarvestHaulRoutes);
			daHaulRoadIDs.Fill(data.rsm_HaulRoadIDs);
		}

		public void UpdateData(dsRSM data)
		{
			daData.Update(data.rsm_HarvestHaulRoutes);
		}

		public IEdgeFlag CreateRoadEdgeFlagFromPoint(IPoint point, int lnversionid, IPoint found)
		{
			IFeature road = null;
			InitSpatial(lnversionid);
			double searchDistance = 50;
			try
			{
				//buffer the point
				ITopologicalOperator topoOp = point as ITopologicalOperator;
				IGeometry pointBuff = topoOp.Buffer(searchDistance);
				//find road edge and percentage along it
			
				IFeatureLayer fLayer = new FeatureLayerClass();
				fLayer.FeatureClass = this.RoadEdge;
				IFeatureSelection fSelection = fLayer as IFeatureSelection;
				ISpatialFilter spatialFilter1 = new SpatialFilterClass();
			
				spatialFilter1.SearchOrder = ESRI.ArcGIS.Geodatabase.esriSearchOrder.esriSearchOrderAttribute;
				//spatialFilter1.SubFields = subFields;
				spatialFilter1.GeometryField = this.RoadEdge.ShapeFieldName;
				spatialFilter1.SpatialRel = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
				spatialFilter1.Geometry = pointBuff;
				fSelection.SelectFeatures(spatialFilter1,ESRI.ArcGIS.Carto.esriSelectionResultEnum.esriSelectionResultNew,true);
				if(fSelection.SelectionSet.Count == 0)
				{
					throw new Exception("No roads within " + searchDistance + " units of this point.");
				}
				road = Utility.FindClosestFeatureToPoint(point, this.RoadEdge, fSelection.SelectionSet.IDs);
			}
			catch(Exception err)
			{
				throw err;
			}
			return CreateRoadEdgeFlagFromRoadFeature(road, point, found);
		}
		
		public int [] GetRouteFromEdgeFlags(ArrayList edgeFlags)
		{
			IEdgeFlag [] edgeflags = new IEdgeFlag[edgeFlags.Count];
			for(int i = 0; i < edgeFlags.Count; i++)
			{
				edgeflags[i] = edgeFlags[i] as IEdgeFlag;
			}
				
			double length = -1;
			return FindPath(edgeflags, out length);
			
		}

		public void CreateRoutes(dsHaulInsertion dsHaulInsertion1, int lnversionid)
		{
			//fill harvesthaulroutes
			dsRSM1.Clear();
			dsRSM1.EnforceConstraints = false;
			daData.Fill(dsRSM1.rsm_HarvestHaulRoutes);
			//fill roads
			daRoads.Fill(dsRSM1.rsm_Roads);
			//fill harvesthaulroutes
			daHaulRoadIDs.Fill(dsRSM1.rsm_HaulRoadIDs);

			//origin only units
			ArrayList harvestUnitsOrig = new ArrayList();
			foreach(dsHaulInsertion.p_rsm_SelectNeedHaulRouteActivityOrigRow harv in dsHaulInsertion1.p_rsm_SelectNeedHaulRouteActivityOrig)
			{
				harvestUnitsOrig.Add(harv.harvestunitname);
			}
			CreateRoutesForOrigin(harvestUnitsOrig, lnversionid);
			
		}

		public string GetHaulRouteName(int haulid)
		{
			string haulname = null;
			try
			{
				cmdGetHaulName.Parameters["@haulid"].Value = haulid;
				cmdGetHaulName.Connection.Open();
				haulname = cmdGetHaulName.ExecuteScalar() as string;
				
			}
			catch(Exception err)
			{
				throw err;
			}
			finally
			{
				cmdGetHaulName.Connection.Close();
			}
			return haulname;
		}

		public void SaveExistingRoute(int [] roadids, int haulid)
		{
			
			//check for contiguous road segments

			//save as currently editing route
			//fill datasets
			this.dsRSM1.EnforceConstraints = false;
			this.daData.Fill(this.dsRSM1.rsm_HarvestHaulRoutes);
			this.daHaulRoadIDs.Fill(this.dsRSM1.rsm_HaulRoadIDs);
			//get haulroute row, update length
			dsRSM.rsm_HarvestHaulRoutesRow haulroute = dsRSM1.rsm_HarvestHaulRoutes.FindByHaulID(haulid);
			int lnversionid = haulroute.LNVersionID;
			InitSpatial(lnversionid);
			haulroute.Length = GetRouteLength(roadids);
			daData.Update(dsRSM1.rsm_HarvestHaulRoutes);

			//get child haulroadid rows
			dsRSM.rsm_HaulRoadIDsRow [] haulroadsids = haulroute.Getrsm_HaulRoadIDsRows();
			//delete them
			foreach (dsRSM.rsm_HaulRoadIDsRow haulroadid in haulroadsids)
			{
				haulroadid.Delete();
			}
			
			//insert new
			AddRoadIDs(roadids, haulid, lnversionid);
			
		}

		private void InitSpatial(int lnversionid)
		{
			//reference layers
			pFeatureworkspace = Utility.GetWorkspaceForVersion(lnversionid) as IFeatureWorkspace;//GlobalSettings.UserWorkspace as IFeatureWorkspace;
			HarvestUnits = pFeatureworkspace.OpenFeatureClass(GlobalSettings.Settings.HarvestUnitFeatureClassName);
			RoadEdge = pFeatureworkspace.OpenFeatureClass(GlobalSettings.Settings.RoadFeatureClassName);
			RMAPsites = pFeatureworkspace.OpenFeatureClass(GlobalSettings.Settings.DeliveryPointFeatureClassName);

			//network
			INetworkWorkspace NetWS = Utility.GetWorkspaceForVersion(lnversionid) as INetworkWorkspace;//GlobalSettings.UserWorkspace as INetworkWorkspace;
			roadNet = NetWS.OpenNetwork(GlobalSettings.Settings.RoadNetworkName, esriNetworkType.esriNTStreetNetwork, esriNetworkAccess.esriNAReadOnly);
			RoadNetElements = roadNet as INetElements;
		}

		private void CreateRoutesForOrigin(ArrayList harvestUnits, int lnversionid)
		{
			InitSpatial(lnversionid);
			
			//continue if more than 0
			if(harvestUnits.Count > 0)
			{
		
				//get cursor of all the specified harvest units
				IQueryFilter queryFilter = new QueryFilterClass();
				queryFilter.WhereClause = Utility.CreateWhereClause(harvestUnits,"HarvestUnit","HarvestUnitName");
				IFeatureCursor harvestUnitCursor = HarvestUnits.Search(queryFilter,true);
		
				//iterate through harvest units
				IFeature harvestUnit = harvestUnitCursor.NextFeature();
				while(harvestUnit != null)
				{
					//get harvest name
					string harvname = harvestUnit.get_Value(harvestUnit.Fields.FindFieldByAliasName("HarvestUnitName")).ToString();
					Debug.WriteLine("creating route for " + harvname);

					//get center of harv
					IPoint harvCenter = ((IArea)harvestUnit.Shape).Centroid;

					//intersect harv unit with roads
					IEnumIDs roadsInHarv = Utility.IntersectFeaturesWithPoly(harvestUnit.Shape, this.RoadEdge);
					
					//find the road feature closest to the centroid of the harvest unit
					IFeature closestRoad = Utility.FindClosestFeatureToPoint(harvCenter, this.RoadEdge, roadsInHarv);

					//find the mainline or primary road features closest to the harvest unit
					IEnumIDs destRoads = Utility.SearchForFeatures(harvestUnit.Shape, 2000, 2000, this.RoadEdge, "RoadEdge.Surface IN (3)"
						, null/*"RoadType"*/);

					//find the closest of these
					IFeature closestPrimary = Utility.FindClosestFeatureToPoint(harvCenter, this.RoadEdge, destRoads);

					//create edgeflag array
					ArrayList edgeflags = new ArrayList();
					IPoint found = null;
					edgeflags.Add(CreateRoadEdgeFlagFromRoadFeature(closestRoad, harvCenter, found));
					edgeflags.Add(CreateRoadEdgeFlagFromRoadFeature(closestPrimary, harvCenter, found));
					/IEdgeFlag [] edgeFlags = {edgeflags[0] as IEdgeFlag, edgeflags[1] as IEdgeFlag};
					double length;
					int [] roadIDList = FindPath(edgeFlags, out length);

					//add the haul route to rsm_harvesthaulroutes
					AddHaulRoute(harvname, harvname + "-", null, roadIDList, lnversionid);

					//go to the next unit
					harvestUnit = harvestUnitCursor.NextFeature();
				}
			}
		}

		public int [] FindPath(IEdgeFlag [] edgeFlags,  out double length)
		{
			int [] roadIDs = {};
			int [] roadIDList = {};
			length = -1;
			try
			{
				ITraceFlowSolverGEN traceSolver = new TraceFlowSolverClass();
				INetSolver solver = traceSolver as INetSolver;
				solver.SourceNetwork = roadNet;
				traceSolver.PutEdgeOrigins(ref edgeFlags);
				INetSchema schema = roadNet as INetSchema;
				INetSolverWeights netWeights = traceSolver as INetSolverWeights;
				INetWeight weight = schema.get_WeightByName("RoadTypeWeight");
				netWeights.FromToEdgeWeight = weight;
				netWeights.ToFromEdgeWeight = weight;
			
				IEnumNetEID roadJunctions = new EnumNetEIDArrayClass();
				IEnumNetEID roadEdges = new EnumNetEIDArrayClass();
				object [] segmentCosts = new object [edgeFlags.Length];

	
				traceSolver.FindPath(esriFlowMethod.esriFMConnected, 
					esriShortestPathObjFn.esriSPObjFnMinSum,
					out roadJunctions, out roadEdges, edgeFlags.Length-1, ref segmentCosts);

				// arrays for storing the featureclass id, object id, and subclass id
				roadIDs = new int[roadEdges.Count];
				int [] FCIDList = new int[roadEdges.Count];
				int [] SCIDList = new int[roadEdges.Count];
			
					
				//index of feature fields of interest
				int lengthField = RoadEdge.FindField("SHAPE_Length");
				//int roadIDField = RoadEdge.FindField("roadid");

				//collect feature ids, roadids, and length by iterating through uid enumeration
				length = 0;
				roadIDList = new int[roadEdges.Count];
				for(int i=0; i < roadEdges.Count; i++)
				{
					RoadNetElements.QueryIDs(roadEdges.Next(),ESRI.ArcGIS.Geodatabase.esriElementType.esriETEdge,out FCIDList[i], out roadIDs[i],out SCIDList[i]);
					roadIDList[i] = RoadEdge.GetFeature(roadIDs[i]).OID;//get_Value(roadIDField).ToString());
					length += double.Parse(RoadEdge.GetFeature(roadIDs[i]).get_Value(lengthField).ToString());
				}
			}
			catch(Exception err)
			{
				throw err;
			}
			return roadIDList;//roadIDs;
		}

		public int AddHaulRoute(string harvestunitname, string haulroutename, string destination, int [] roadIDs, int lnversionid)
		{
			int haulid = -1;
			//refresh the dataset
			dsRSM1.Clear();
			FillData(dsRSM1);
				
			//check for existing routes for this origin
			DataRow [] existing = dsRSM1.rsm_HarvestHaulRoutes.Select("harvestunitname = '" + harvestunitname + "'");

			//make a new row
			dsRSM.rsm_HarvestHaulRoutesRow haul = dsRSM1.rsm_HarvestHaulRoutes.Newrsm_HarvestHaulRoutesRow();
			//edit attributes
			haul.BeginEdit();
			haul.HarvestUnitName = harvestunitname;
			haul.DestinationID = destination;
			haul.HaulRouteName = haulroutename;
			haul.Length = GetRouteLength(roadIDs);
			haul.LNVersionID = lnversionid;

			if(existing.Length == 0)
			{
				haul.DefaultRoute = true;
			}
			else
			{
				haul.DefaultRoute = false;
			}
			haul.EndEdit();
			dsRSM1.rsm_HarvestHaulRoutes.Addrsm_HarvestHaulRoutesRow(haul);
			
			//update
			daData.Update(dsRSM1.rsm_HarvestHaulRoutes);
			dsRSM1.rsm_HarvestHaulRoutes.AcceptChanges();
			Debug.WriteLine("inserted haulroute for " + harvestunitname + "-" + destination);

			//add road ids
			AddRoadIDs(roadIDs, haul.HaulID, lnversionid);
			haulid = haul.HaulID;


			return haulid;

		}

>
		private void AddRoadIDs(int [] roadIDs, int haulID, int lnversionid)
		{
			try
			{
			
				
				for(int i = 0; i < roadIDs.Length; i++)
				{
					if(dsRSM1.rsm_HaulRoadIDs.Select("RoadID = " + roadIDs[i] + " and HaulID = " + haulID).Length == 0)
					{
						//make a new row
						dsRSM.rsm_HaulRoadIDsRow haul = dsRSM1.rsm_HaulRoadIDs.Newrsm_HaulRoadIDsRow();
						//edit attributes
						haul.BeginEdit();
						haul.HaulID = haulID;
						haul.RoadID = roadIDs[i];
						haul.LNVersionID = lnversionid;
						haul.EndEdit();
						dsRSM1.rsm_HaulRoadIDs.Addrsm_HaulRoadIDsRow(haul);
					}
				}
				//update
				daHaulRoadIDs.Update(dsRSM1.rsm_HaulRoadIDs);
				dsRSM1.rsm_HaulRoadIDs.AcceptChanges();
				Debug.WriteLine("inserted roadids associated with haulID: " + haulID.ToString());

				
			}
			catch(Exception err)
			{
				//throw err;
				Debug.WriteLine("insert of roadids failed because: " + err.Message);
			}
		}


		private IEdgeFlag CreateRoadEdgeFlagFromRoadFeature(IFeature road, IPoint closestPoint, IPoint foundPoint)
		{
			ICurve roadcurve = road.Shape as ICurve;
			double distanceAlong = 0;
			double distanceFrom = 0;
			bool rightSide = false;
			roadcurve.QueryPointAndDistance(esriSegmentExtension.esriNoExtension, closestPoint, true, foundPoint, ref distanceAlong, ref distanceFrom, ref rightSide);
			double position = distanceAlong/roadcurve.Length;

			//create flag
			IEdgeFlag edgeFlag1 = new EdgeFlagClass();
			INetFlag netFlag1 = edgeFlag1 as INetFlag;
			netFlag1.ClientClassID = 0;
			netFlag1.ClientID = 0;
			//netFlag1.Label = "StartFlag";
			netFlag1.UserClassID = RoadEdge.ObjectClassID;
			netFlag1.UserID = road.OID;
			edgeFlag1.Position = float.Parse(position.ToString());
			edgeFlag1.TwoWay = true;

			return edgeFlag1;

		}

		public ArrayList GetRoadIDs(int haulid)
		{
			ArrayList roads = new ArrayList();
			dsRSM1.Clear();
			dsRSM1.EnforceConstraints = false;
			daHaulRoadIDs.Fill(dsRSM1.rsm_HaulRoadIDs);
			DataRow [] roadids = dsRSM1.rsm_HaulRoadIDs.Select("haulid = " + haulid);
			foreach(dsRSM.rsm_HaulRoadIDsRow road in roadids)
			{
				roads.Add(road.RoadID);
			}
			return roads;

		}
		
		public int [] GetRoadIDsFromOIDs(int [] OIDs)
		{
			

			int roadIDField = RoadEdge.FindField("roadid");
			int [] roadIDs = new int[OIDs.Length];
			for(int i = 0; i < OIDs.Length; i++)
			{
				roadIDs[i] = int.Parse(RoadEdge.GetFeature(OIDs[i]).get_Value(roadIDField).ToString());
			}
			return roadIDs;

		}

		public void FillHaulRouteForUnit(string harvestunitname, int lnversionid, dsRSM data)
		{
			data.EnforceConstraints = false;
			daGetHaulRoutesForUnit.SelectCommand.Parameters["@harvestunitname"].Value = harvestunitname;
			daGetHaulRoutesForUnit.SelectCommand.Parameters["@lnversionid"].Value = lnversionid;
			data.rsm_HarvestHaulRoutes.Clear();
			daGetHaulRoutesForUnit.Fill(data.rsm_HarvestHaulRoutes);


		}

		private double GetRouteLength(int [] roadids)
		{
			double length = 0;
			int lengthField = RoadEdge.FindField("SHAPE_Length");
			for(int i=0; i < roadids.Length; i++)
			{
				length += double.Parse(RoadEdge.GetFeature(roadids[i]).get_Value(lengthField).ToString());
			}
			return length * 0.000621371192;
		}
	
		
	}

} 
