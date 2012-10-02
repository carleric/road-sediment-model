using System;
//using WiseOwl.Statistics;
using Microsoft.Office.Core;
using System.Collections;

namespace stillwatersci.rsm.lib
{
	/// <summary>
	/// Summary description for Runoff.
	/// </summary>
	public class RunoffManager
	{
		//private int counter1, counter2, counter3, counter4, counter5, r, s, t, n, m, i, j, k;
		private double alpha, beta, gamma;
		//threshold, icap, depth, lag;
		//private int time_step;
		private double h;
		private double excess_rain;
		//private double [,] out_temp;
		//private double [] flow_out;
		//private double [] rain_in;
		private double [] f;
		private double fsum, fsumtotal;
		private double [] g;
//		private double [] ditch_flow;
//		private double [] real_flow;
//		private double [] cube_diff;
//		private double sum_cube, sum_cube2, sc;
		
		private const int STEPS = 144000;//for 24 hours * 60 minutes * 1000 = 144250 one thousanth minutes jumped forward 250 steps
		private const int MINS5 = 288;


		public RunoffManager()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		/// <summary>
		/// builds gamma distribution to specified alpha and beta, and stores in local array
		/// </summary>
		public void InitGammaDistribution(double alpha, double beta)
		{
			//GammaDeviate gammaDev = new GammaDeviate(1);
			Excel.Application app = new Excel.ApplicationClass();
			
			//compute integral at 0.01 minute interval
			this.alpha = alpha;
			this.beta = beta;
			this.gamma = Math.Exp(app.WorksheetFunction.GammaLn(alpha));

			
			h = 0.01;
			f = new double[STEPS];
			for(int i = 0; i < STEPS; i++)
			{
				f[i] = (1 / gamma / Math.Pow(beta, alpha)) * Math.Pow(h, (alpha - 1)) * Math.Exp(-(h) / beta); //equation 1
				h += 0.01;
			}

			fsum = 0;
			fsumtotal = 0;
			g = new double[MINS5];
			//sum first 2.5 minutes and put into first slot of gamma distribution array
//			r = 0;
//			for(r = 0; r < 250; r++)
//			{
//				fsumtotal += f[r] + 0.01;
//			}
//			g[0] = fsumtotal;

			int t = 0; //counter 0 thru 288
			int s = 0;
			for(int r = 0; r < STEPS; r = s)
			{				
				for(s = r; s < 500 + r; s++)
				{
					fsum += f[s] * 0.01; //getting area under curve
					fsumtotal += f[s] * 0.01; //will become very close to 1 when finished
				}

				g[t] = fsum;
				t++;
				fsum = 0;
			}
			//m = t - 1;//length of transfer function array
			
		}

		public double [] FillDischarge(double [] rain_in, double threshold, int lag, double icap)
		{
			int n = rain_in.Length;
			int m = MINS5;

			//flow_out is discharge resultset
			double [] flow_out = new double[n + m + lag - 1];

			//find storms in rain record
			ArrayList storms = FindStorms(rain_in, threshold);
			foreach(Storm storm in storms)
			{
				//get discharge for each storm
				GetDischarge(storm, rain_in, flow_out, threshold, lag, icap);
			}

//			out_temp = new double[n, n + m + lag - 1];
//			for(int i = 0; i < n; i++)
//			{
//				if(rain_in[i] > threshold)
//				{	
//					for(int j = 0; j < m; j++)
//					{
//						out_temp[i, j + i + lag] = (rain_in[i] - icap) * g[j];
//						if(out_temp[i, j + i + lag] < 0)
//							out_temp[i, j + i + lag] = 0;
//					}	
//				}
//			}
//
//			//sum up unit hydrographs at each time step
//			flow_out = new double[n + m + lag - 1];
//			for(int j = 0; j < n + m + lag - 1; j++)
//			{
//				for(int i = 0; i < n; i++)
//				{
//					flow_out[j] += out_temp[i, j];
//				}
//			}

			return flow_out;
		}

		private void GetDischarge(Storm storm, double [] rain_in, double [] flow_out, double threshold, int lag, double icap)
		{
			int n = storm.Length;
			int m = MINS5;

			double [,] out_temp = new double[n, n + m + lag - 1];
			for(int i = 0; i < n; i++)
			{
				if(rain_in[i + storm.Start] > threshold)
				{	
					for(int j = 0; j < m; j++)
					{
						out_temp[i, j + i + lag] = (rain_in[i + storm.Start] - icap) * g[j];
						if(out_temp[i, j + i + lag] < 0)
							out_temp[i, j + i + lag] = 0;
					}	
				}
			}

			//sum up unit hydrographs at each time step
			for(int j = 0; j < n + m + lag - 1; j++)
			{
				for(int i = 0; i < n; i++)
				{
					flow_out[j + storm.Start] += out_temp[i, j];
				}
			}
		}

		private ArrayList FindStorms(double [] rain_in, double threshold)
		{
			//scan rain record for storms, add storm index intervals to arraylist
			ArrayList storms = new ArrayList();
			int start = 0;
			int end = 0;
			int count = 0;
			bool stormstarted = false;

			for(int i = 0; i < rain_in.Length; i++)//for each 5 min rain value
			{
				if(!stormstarted && rain_in[i] >= threshold)//if value is above threshold and a storm hasn't been detected, start it
				{
					start = i;
					count ++;
					stormstarted = true;
				}
				else if(stormstarted && rain_in[i] >= threshold)//if storm has started and still above value, increment count and keep going
				{
					count ++;
				}
				else if(stormstarted && rain_in[i] < threshold && count > 3)//if storm has started, lasted more than 3 steps and is now below threshold, save the storm
				{
					end = i;
					stormstarted = false;
					count = 0;
					storms.Add(new Storm(start, end));
				}
				else if(stormstarted && rain_in[i] < threshold && count <= 3)//if storm ends, and less than 3 steps, don't use it
				{
					stormstarted = false;
					count = 0;
				}

			}

			return storms;

		}

		private struct Storm
		{
			private int start, end;

			public Storm(int start, int end)
			{
				this.start = start;
				this.end = end;
			}

			public int Start
			{
				get
				{
					return start;
				}
			}

			public int End
			{
				get
				{
					return end;
				}
			}

			public int Length
			{
				get
				{
					return end - start;
				}
			}
		}
	}
}
