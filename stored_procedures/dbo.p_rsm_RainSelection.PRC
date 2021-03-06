SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS ON 
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[p_rsm_RainSelection]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[p_rsm_RainSelection]
GO

/**********************
*
* Description: For populating the experimental rain selection dialog
* Programmer: Carl Bolstad
* Revisions:
*	Date:	Who:	Description:
*
* Copyright © 2004 Stillwater Sciences, All Rights Reserved.
*
***********************/

CREATE procedure p_rsm_RainSelection
(@runid int) as

select 
b.monitoringpointid
,c.lithocode
,min(d.raindatetime) as PastAvailableFrom
,max(d.raindatetime) as PastAvailableTo
,(select starttime from rsm_modelruns where runid = @runid) as runstart
,(select endtime from rsm_modelruns where runid = @runid) as runend
,datediff(day,(select starttime from rsm_modelruns where runid = @runid), min(d.raindatetime)) as PreviousRequired
,datediff(day, max(d.raindatetime),(select endtime from rsm_modelruns where runid = @runid)) as FutureRequired

from 

rsm_deliverypointactivity a
left join rsm_deliverypoints b on a.deliverypointid = b.deliverypointid
left join rsm_monitoringpoints c on b.monitoringpointid = c.monitoringpointid
left join rsm_rainfall d on c.monitoringpointid = d.monitoringpointid

where a.runid = @runid

group by
b.monitoringpointid
,c.lithocode

GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

