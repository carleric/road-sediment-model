SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS ON 
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[p_rsm_LoadHarvActivity]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[p_rsm_LoadHarvActivity]
GO

/**********************
*
* Description: Loads Harvest Activity Information from rsm_ActualActivity and rsm_ProjectedActivity into
* specified run, using specified runid and projectionid.  loads actual data from run startdate upto start of
* projected data
* Programmer: Carl Bolstad
* Revisions:
*	Date:	Who:	Description:
*
* Copyright © 2004 Stillwater Sciences, All Rights Reserved.
*
***********************/

CREATE PROCEDURE p_rsm_LoadHarvActivity

	(
		@runID int,
		@projectionid varchar(30),
		@lnversionid int
	)

AS

	/*
	BEGIN
	SET NOCOUNT ON
		
	-- local error handling variables
	DECLARE @li_return_value int		-- procedure return value
	DECLARE @li_issue_error int		-- flag indicating whether error condition has occured
	DECLARE @ls_error_msg varchar(255)	-- used to build local error message
	DECLARE @li_error int 			-- user to hold @@ERROR global variable
	SET NOCOUNT ON;
	*/
	--delete any previously loaded
	delete from rsm_HarvestActivity where runID = @runID
	

	--load actual
	insert into rsm_HarvestActivity
	(runID, HarvestUnitName, DailyProduction, Loads, HarvestDate, Actual, lnversionid)
	SELECT     @runID, HarvestUnitName, sum(DailyProduction), sum(Loads), ActivityDate, 1, @lnversionid
		--harvestunitname, activitydate, count(harvestunitname)
	FROM       rsm_ActualActivity a

	where a.ActivityType = 'Harvest'
	and a.activitydate > (select starttime from rsm_modelruns where runid = @runID)
	and a.activitydate < (select endtime from rsm_modelruns where runid = @runID)
	group by harvestunitname, activitydate
	          
	 
	  
	--load projected        
	insert into rsm_HarvestActivity
	(runID, HarvestUnitName, DailyProduction, Loads, HarvestDate, lnversionid)
		select @runID, b.HarvestUnitName, b.DailyProduction,
	b.Loads, b.ActivityDate, @lnversionid
	from rsm_ProjectedActivity b
	where b.projectionid = @projectionid
	and b.ActivityDate > (select max(activitydate) from rsm_actualactivity) 
	and b.ActivityDate < (select endtime from rsm_modelruns where runid = @runID)
	and b.ActivityType = 'Harvest'
	
	/*
	-- ===========================================================
	-- capture error count in local variable
	SELECT @li_error =@@ERROR
	
	IF @li_error <> 0
	BEGIN
		-- Error has been encountered so format the stored procedure output error variables
		SELECT @ls_error_msg = 'Error encountered selecting records.' + 
				' Error encountered was ' + convert(varchar(12),@li_error)
		SELECT @li_issue_error = 1
		GOTO  ISSUEERROR
	END
	
	
	
	SELECT @li_return_value = 0
	
	GOTO DORETURN
	
	ISSUEERROR:
	
	IF @li_issue_error = 1
	BEGIN
		IF @@TRANCOUNT > 0 
		BEGIN
			ROLLBACK TRANSACTION
		END
	
	
		-- Error has been encountered so handle error according to local practice
		SELECT @ls_error_msg = 'Error occured in ' + object_name(@@PROCID) + ' ' + @ls_error_msg
	
		RAISERROR (@ls_error_msg, 16, 1)
		SELECT @li_return_value = -1
	END
	
	DORETURN:
	
	IF @@TRANCOUNT > 0 
	BEGIN
		COMMIT TRANSACTION
	END
	RETURN @li_return_Value
END
*/


GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

