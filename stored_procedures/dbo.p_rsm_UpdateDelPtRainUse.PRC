SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS ON 
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[p_rsm_UpdateDelPtRainUse]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[p_rsm_UpdateDelPtRainUse]
GO

/**********************
*
* Description: Experimental: To facilitate population of rsm_deliverypointresults
* Programmer: Carl Bolstad
* Revisions:
*	Date:	Who:	Description:
*
* Copyright © 2004 Stillwater Sciences, All Rights Reserved.
*
***********************/


CREATE procedure p_rsm_UpdateDelPtRainUse
(@runid int)
as

declare @del_ctr int
declare @deliveryptid int
declare @monpttouse int
declare @raindatetouse datetime
declare @activitydate datetime
declare @rainyearavail int

update rsm_deliverypointactivity
set monitoringpointtouse = b.monitoringpointid
from
rsm_deliverypointactivity a left join rsm_deliverypoints b on a.deliverypointid = b.deliverypointid
where runid = @runid





declare del_cursor cursor
for
	select deliverypointid,  monitoringpointtouse, raindatetouse, activitydate
	from dbo.rsm_deliverypointactivity
	where runID = @runid
	
--select @del_ctr = (select count(deliverypointid) from rsm_deliverypointactivity where runid = @runid)
open del_cursor

fetch del_cursor into @deliveryptid, @monpttouse, @raindatetouse, @activitydate
while @@fetch_status = 0--@del_ctr > 0 
begin
	
	exec p_rsm_GetMonPtRainUsage  @monpttouse, @activitydate, @raindatetouse, @monpttouse 
	/*update rsm_deliverypointactivity
	set monitoringpointtouse = @monpttouse, raindatetouse = @raindatetouse
	where runid = @runid and deliverypointid = @deliveryptid and activitydate = @activitydate*/
	print @raindatetouse

	fetch next from del_cursor into @deliveryptid, @monpttouse, @raindatetouse, @activitydate
	select @del_ctr = @del_ctr - 1
end
close del_cursor
deallocate del_cursor




GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

