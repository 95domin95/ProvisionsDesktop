USE [Provisions]
GO
/****** Object:  StoredProcedure [dbo].[p_add_provision]    Script Date: 10/4/2019 23:18:52 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Dominik Wycisło
-- Create date: 20.09.2019
-- Description:	Provision desktop login
-- =============================================
CREATE PROCEDURE [dbo].[p_add_provision]
	@Id nvarchar(100),
	@Name nvarchar(100),
	@Description nvarchar(100) = null,
	@Weight int = 1,
	@StartDate datetime = null
AS
BEGIN
	if @StartDate is null begin
		set @StartDate = GETDATE()
	end

	insert into Provisions (Id, Name, [Description], [Weight], StartDate, UserId) values(NEWID(), @Name, @Description, @Weight, @StartDate, @Id)
END

GO
/****** Object:  StoredProcedure [dbo].[p_get_statuses]    Script Date: 10/4/2019 23:18:52 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Dominik Wycisło
-- Create date: 20.09.2019
-- Description:	Provision desktop login
-- =============================================
CREATE PROCEDURE [dbo].[p_get_statuses]
AS
BEGIN
	select Name as Name, Id as Id from Statuses;
END
GO
/****** Object:  StoredProcedure [dbo].[p_get_user_days]    Script Date: 10/4/2019 23:18:52 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Dominik Wycisło
-- Create date: 20.09.2019
-- Description:	Provision desktop login
-- =============================================
CREATE PROCEDURE [dbo].[p_get_user_days]
	@UserId nvarchar(100),
	@ProvisionId nvarchar(100) = null
AS
BEGIN
	select pd.Id as Id, d.[Date] as [Date], p.Name as ProvisionName,
	 d.[Description] as 'Description', s.Name as [Status] from [Days] as d 
	inner join AspNetUsers as u on d.UserId=u.Id
	inner join ProvisionDays as pd on d.Id=pd.DayId
	inner join Provisions as p on p.Id=pd.ProvisionId
	inner join Statuses as s on s.Id=pd.StatusId
	where (@ProvisionId is null or(p.Id=Convert(uniqueidentifier, @ProvisionId)));
END
GO
/****** Object:  StoredProcedure [dbo].[p_login]    Script Date: 10/4/2019 23:18:52 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Dominik Wycisło
-- Create date: 20.09.2019
-- Description:	Provision desktop login
-- =============================================
CREATE PROCEDURE [dbo].[p_login]
	@UserName nvarchar(100), 
	@PasswordHash nvarchar(100)
AS
BEGIN
	select top 1 * from AspNetUsers where @UserName=UserName and @PasswordHash=PasswordHash
END

GO
/****** Object:  StoredProcedure [dbo].[p_provisions_list]    Script Date: 10/4/2019 23:18:52 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Dominik Wycisło
-- Create date: 20.09.2019
-- Description:	Provision desktop login
-- =============================================
CREATE PROCEDURE [dbo].[p_provisions_list]
	@UserId nvarchar(100)
AS
BEGIN
	select [Name] as 'Name', [Description] as 'Description', StartDate as StartDate, p.Id as Id from Provisions as p inner join AspNetUsers as u
		on p.UserId=u.Id where u.Id=@UserId;
END

GO
/****** Object:  StoredProcedure [dbo].[p_remove_provision]    Script Date: 10/4/2019 23:18:52 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[p_remove_provision]
	@Name nvarchar(100)
AS
BEGIN

	set NOCOUNT off;
	delete from Provisions where Name=@Name
	if @@ROWCOUNT > 0 begin
		return 0
	end

	if @@ROWCOUNT = 0 begin
		return -1
	end
END
GO
/****** Object:  StoredProcedure [dbo].[p_save_day_changes]    Script Date: 10/4/2019 23:18:52 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Dominik Wycisło
-- Create date: 20.09.2019
-- Description:	Provision desktop login
-- =============================================
CREATE PROCEDURE [dbo].[p_save_day_changes]
	@Id varchar(100), 
	@Date DateTime,
	@ProvisionName nvarchar(100),
	@Status varchar(100),
	@UserId varchar(100),
	@Description varchar(max)=null
AS
BEGIN
	DECLARE @CurrentId varchar(100);
	DECLARE @CurrentDate DateTime;
	DECLARE @CurrentProvisionName nvarchar(100);
	DECLARE @CurrentStatus varchar(100);
	DECLARE @NewProvisionId varchar(100);
	DECLARE @NewDayID varchar(100);
	DECLARE @NewStatusId varchar(100);
	DECLARE @Conflict bit = 0;
	DECLARE @DayId varchar(100);

	select top 1 @CurrentId=d.Id, @CurrentDate=d.[Date], @CurrentProvisionName=p.Name,
		@CurrentStatus=s.Name from [Days] as d 
		inner join AspNetUsers as u on d.UserId=u.Id
		inner join ProvisionDays as pd on d.Id=pd.DayId
		inner join Provisions as p on p.Id=pd.ProvisionId
		inner join Statuses as s on s.Id=pd.StatusId
		where pd.Id=@Id;

	select top 1 @NewProvisionId=Id from Provisions where Name=@ProvisionName;
	if @Status is not null and @Status!='' begin
		select top 1 @NewStatusId=Id from Statuses where Name=@Status;
	end

	if @CurrentId is null and @NewProvisionId is not null begin
		SET @NewDayID=NEWID();

		if @Status is null begin
			select top 1 @Status=Id from Statuses where Name='Wykonano'
		end

		if @Date is null begin
			set @Date = GETDATE()
		end

		select top 1 @Conflict=Count(*) from [Days] as d inner join ProvisionDays as pd on d.Id=pd.DayId
			where convert(date, d.[Date])=CONVERT(date, @Date) and pd.ProvisionId=@NewProvisionId;

		if @Conflict=0 begin
			insert into [Days] (Id, [Date], Difficulty, [Description], UserId) values (@NewDayID, @Date, 0, '', @UserId);
			insert into [ProvisionDays] (Id, DayId, ProvisionId, StatusId) values (NEWID(), @NewDayID, @NewProvisionId, @NewStatusId)
		end
		return
	end

	select @DayId=Id from ProvisionDays where @Id=Id;

	if @CurrentDate!=@Date begin
		update [Days] set [Date]=@Date where Id=@CurrentId
	end

	if @ProvisionName=@CurrentProvisionName and @NewProvisionId is not null begin
		select top 1 @Conflict=Count(*) from ProvisionDays
			where ProvisionId=@NewProvisionId and DayId=@DayId;
		update [ProvisionDays] set ProvisionId=@NewProvisionId where Id=@Id;
	end
	
	if @Status!=@CurrentStatus and @NewStatusId is not null begin
		update [ProvisionDays] set StatusId=@NewStatusId where Id=@Id;
	end	

	if @Description is not null begin
		update [Days] set [Description]=@Description where Id=@CurrentId;
	end
END
GO
