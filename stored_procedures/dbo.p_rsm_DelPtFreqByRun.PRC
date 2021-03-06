SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS ON 
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[p_rsm_DelPtFreqByRun]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[p_rsm_DelPtFreqByRun]
GO

create procedure p_rsm_DelPtFreqByRun(@runid int) as

DECLARE @sedcount TABLE (delpt int, totalsed float, category varchar(20)) INSERT INTO @sedcount (delpt, totalsed, category)

SELECT deliverypointid, SUM(sediment) AS totalsed, 
             'category' = CASE WHEN SUM(sediment) BETWEEN 0 AND 
             999 THEN '0-999' WHEN SUM(sediment) BETWEEN 1000 AND 
             1999 THEN '1000-1999' WHEN SUM(sediment) BETWEEN 2000 AND 
             2999 THEN '2000-2999' WHEN SUM(sediment) BETWEEN 3000 AND 
             3999 THEN '3000-3999' WHEN SUM(sediment) BETWEEN 4000 AND 
             4999 THEN '4000-4999' WHEN SUM(sediment) BETWEEN 5000 AND 
             5999 THEN '5000-5999' WHEN SUM(sediment) BETWEEN 6000 AND 
             6999 THEN '6000-6999' WHEN SUM(sediment) BETWEEN 7000 AND 
             7999 THEN '7000-7999' WHEN SUM(sediment) BETWEEN 8000 AND 
             8999 THEN '8000-8999' WHEN SUM(sediment) BETWEEN 9000 AND 
             9999 THEN '9000-9999' WHEN SUM(sediment) BETWEEN 10000 AND 
             10999 THEN '10000-10999' WHEN SUM(sediment) BETWEEN 11000 AND 
             11999 THEN '11000-11999' WHEN SUM(sediment) BETWEEN 12000 AND 
             12999 THEN '12000-12999' WHEN SUM(sediment) BETWEEN 13000 AND 
             13999 THEN '13000-13999' WHEN SUM(sediment) BETWEEN 14000 AND 
             14999 THEN '14000-14999' WHEN SUM(sediment) BETWEEN 15000 AND 
             15999 THEN '15000-15999' WHEN SUM(sediment) BETWEEN 16000 AND 
             16999 THEN '16000-16999' WHEN SUM(sediment) BETWEEN 17000 AND 
             17999 THEN '17000-17999' WHEN SUM(sediment) BETWEEN 18000 AND 
             18999 THEN '18000-18999' WHEN SUM(sediment) BETWEEN 19000 AND 
             19999 THEN '19000-19999' WHEN SUM(sediment) BETWEEN 20000 AND 
             20999 THEN '20000-20999' WHEN SUM(sediment) BETWEEN 21000 AND 
             21999 THEN '21000-21999' WHEN SUM(sediment) BETWEEN 22000 AND 
             22999 THEN '22000-22999' WHEN SUM(sediment) BETWEEN 23000 AND 
             23999 THEN '23000-23999' WHEN SUM(sediment) BETWEEN 24000 AND 
             24999 THEN '24000-24999' WHEN SUM(sediment) BETWEEN 25000 AND 
             25999 THEN '25000-25999' WHEN SUM(sediment) BETWEEN 26000 AND 
             26999 THEN '26000-26999' WHEN SUM(sediment) BETWEEN 27000 AND 
             27999 THEN '27000-27999' WHEN SUM(sediment) BETWEEN 28000 AND 
             28999 THEN '28000-28999' WHEN SUM(sediment) BETWEEN 29000 AND 
             29999 THEN '29000-29999' ELSE '30000+' END
FROM   rsm_deliverypointactivity
WHERE runid = @runid
GROUP BY deliverypointid
                 SELECT runname, category, COUNT(category) AS freq
                 FROM  @sedcount,
                                    (SELECT runname
                                     FROM   rsm_modelruns
                                     WHERE runid = @runid) a
    GROUP BY category, runname

GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

