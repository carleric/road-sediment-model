SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS ON 
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[p_rsm_InsertPastRainfall]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[p_rsm_InsertPastRainfall]
GO

/**********************
*
* Description: Experimental, to insert past rainfall (where available) for a run
* Programmer: Carl Bolstad
* Revisions:
*	Date:	Who:	Description:
*
* Copyright © 2004 Stillwater Sciences, All Rights Reserved.
*
***********************/

CREATE procedure p_rsm_InsertPastRainfall
(
@runid int,
@deliverypointid int,
@monitoringpointid int,
@lnversionid int,
@activitydatestart datetime,
@activitydateend datetime,
@rainfallthreshold decimal(10,6),
@writes int output
)
as 



insert into rsm_deliverypointresults
(runid, lnversionid, activitydate, deliverypointid, [datetime], rainfall)

select
@runid, @lnversionid, @activitydatestart, @deliverypointid, RainDateTime, value
from 
rsm_Rainfall
where 
monitoringpointid = @monitoringpointid
and lnversionid = @lnversionid
and RainDateTime > @activitydatestart 
and RainDateTime <= @activitydateend
and value > @rainfallthreshold

select @writes = @@TOTAL_WRITE
GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

