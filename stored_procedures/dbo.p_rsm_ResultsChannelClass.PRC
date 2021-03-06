SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS ON 
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[p_rsm_ResultsChannelClass]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[p_rsm_ResultsChannelClass]
GO

/**********************
*
* Description: For experimental sediment report based on rsm_deliverypointactivity table
* Programmer: Carl Bolstad
* Revisions:
*	Date:	Who:	Description:
*
* Copyright © 2004 Stillwater Sciences, All Rights Reserved.
*
***********************/

CREATE procedure p_rsm_ResultsChannelClass
(@runid int) as

select
b.lithocode as LTU, 
case when d.channelclass is null then 'Unknown' else d.channelclass end as channelclass,
--b.deliverypointname, 
--a.activitydate, 
--a.rainfall, 
--a.ditchflow, 
--a.sediment as sedimentGrams,
--a.sediment/1.5 as sedimentCM3, 
--a.sediment/1.5*0.0000013 as sedimentYD3, 
sum(a.sediment/1.8*0.0000013) as totalSedYD3ChannelClass,
sum(a.sediment/1.8*0.0000013/d.lengthmiles) as totalSedYD3PermileChannelClass, 
(d.surfaceerosion*d.lengthmiles)/365*datediff(day, f.starttime, f.endtime) as normalizedCCAlloc,
sum(a.sediment/1.8*0.0000013)/(d.surfaceerosion*d.lengthmiles)/365*datediff(day, f.starttime, f.endtime)*100 as PercentOfCCBudget


from 
rsm_deliverypointactivity a
join rsm_deliverypoints b on a.deliverypointid = b.deliverypointid
join rsm_hydro c on b.hydrosegid = c.hydrosegid
left join rsm_channelclass d on c.channelclass = d.channelclass
--join rsm_lithotopounits e on left(c.channelclass,3) = e.lithocode
join rsm_modelruns f on a.runid = f.runid

where 
a.runid = @runid
group by b.lithocode, d.channelclass, d.surfaceerosion, d.lengthmiles, f.starttime, f.endtime



GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

