using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections;
using System.Diagnostics;

namespace stillwatersci.rsm.lib
{
	/// <summary>
	/// Summary description for BinData.
	/// 
	/// Description: For translating raw rain guage data into rain intensities at specified 
	/// time interval. Ported from VBA function in Excel originally written by Richard Keim at 
	/// the Forest Engineering Dept at Oregon State University.
	/// Programmer: Carl Bolstad
	/// Revisions:
	///		Date:	Who:	Description:
	///		
	///	Copyright © 2004 Stillwater Sciences, All Rights Reserved.
	///	
	/// </summary>
	public class BinData
	{
		private int timeStep;
		private double tipval;

		public BinData(/*DateTime start_time, DateTime end_time,*/ int timeStep, double tipval)
		{
			this.timeStep = timeStep;
			this.tipval = tipval;
		}
	
		public void Compute(DateTime [] data, out DateTime [] RainTime, out double [] RainIntensity)
		{
			//determine number of steps from start_time and end_time
			DateTime start = data[0];
			DateTime end = data[data.Length-1];
			TimeSpan interval = end - start;
			int steps = Convert.ToInt32(Math.Floor(interval.TotalMinutes)/timeStep);
			int a = 0;
			int count = data.Length;
			double binvol;
			//allocate resultset: DateTime, volume
			RainTime = new DateTime[steps];
			RainIntensity = new double[steps];

			//use start_time in constructor to determine first lower bin boundary
			DateTime t1 = data[0];
			DateTime t2 = t1.AddMinutes(timeStep);
	
			TimeSpan Span1;
			TimeSpan Span2;

			//for each time step (bin) calculate volume
			for(int i = 0; i < steps; i++)
			{
				while(t1 >= data[a])
				{
					a++;
				}
				if(a == count - 1)
				{
					Debug.WriteLine("end of data array reached with " + (steps - i) + " steps to go.");
				}
				if(t2 <= data[a])//if upper limit of bin is before the current tip event
				{
					Span1 = (data[a] - data[a-1]);//timespan between previous and current tip event
					Span2 = t2 - t1;//timespan of bin
					binvol = (tipval / Span1.TotalMinutes) * Span2.TotalMinutes;
				}
				else if(t2 <= data[a+1])//if upper limit of bin is before the next tip event
				{
					Span1 = (data[a+1] - data[a]);//timespan between next and current tip event
					Span2 = t2 - data[a];//timespan betwen upper limit of bin and current tip event
					binvol = (tipval / Span1.TotalMinutes * Span2.TotalMinutes);
					Span1 = (data[a] - data[a-1]);//timespan between previous and current tip event
					Span2 = data[a] - t1;//timespan between current tip event and lower limit of bin
					binvol += (tipval / Span1.TotalMinutes * Span2.TotalMinutes);
				}
				else //if tip occurred within this bin
				{
					Span1 = (data[a] - data[a-1]);//timespan between previous and current tip event
					Span2 = (data[a] - t1);//timespan between current tip event and lower limit of bin
					binvol = (tipval / Span1.TotalMinutes * Span2.TotalMinutes);
					while(t2 > data[a + 1])
					{
						if(data[a + 1] == data[a])
						{
							binvol += tipval;
						}
						else
						{
							Span1 = (data[a+1] - data[a]);//timespan between next and current tip event
							Span2 = (data[a+1] - data[a]);//timespan between next and current tip event
							binvol += (tipval / Span1.TotalMinutes * Span2.TotalMinutes);
						}
						a++;
						if(a == count - 1) break;
					}
					//if(a == count - 1) break;
					Span1 = (data[a+1] - data[a]);//timespan between next and current tip event
					Span2 = t2 - data[a];//timespan betwen upper limit of bin and current tip event
					binvol += (tipval / Span1.TotalMinutes * Span2.TotalMinutes);
				}
				RainTime[i] = t1;
				if(RainTime[i].Year == 1)
					Debug.WriteLine(t1.Year + " " + RainTime[i].Year);
				//Debug.WriteLine(RainTime[i].ToShortDateString());
				RainIntensity[i] = binvol;
				t1 = t2;
				t2 = t2.AddMinutes(timeStep);
				
				if(i * timeStep > interval.TotalMinutes) break;
				if(t2 > data[data.Length - 1]) break;
				//if(a == count - 1) break;
			}
		}	

	}



	public struct TimeValue
	{
		private DateTime time;
		private double val;

		public TimeValue(DateTime time, double val)
		{
			this.time = time;
			this.val = val;
		}

		public DateTime Time
		{
			get
			{
				return time;
			}
			set
			{
				this.time = value;
			}
		}

		public double Value
		{
			get
			{
				return val;
			}
			set
			{
				this.val = value;
			}
		}

		public decimal DecVal
		{
			get
			{
				if(DecIsValid(val))
				{
					return decimal.Parse(val.ToString());
				}
				else
				{
					//Debug.WriteLine(val.ToString());
					return 0;
				}
			}
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
	}
}
