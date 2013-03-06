CREATE DATABASE  IF NOT EXISTS `serversidedebugger` /*!40100 DEFAULT CHARACTER SET latin1 */ //

USE `serversidedebugger` //



CREATE FUNCTION `Peek`( pDebugSessionId int ) RETURNS varchar(50) CHARSET latin1
begin

    declare nextId int;
    declare returnValue varchar( 50 );
    
    set nextId = ( select max( `serversidedebugger`.`DebugCallStack`.`Id` ) from `serversidedebugger`.`DebugCallStack` where `serversidedebugger`.`DebugCallStack`.`DebugSessionId` = pDebugSessionId );
    set returnValue = ( select RoutineName from `serversidedebugger`.`DebugCallStack` 
        where ( `serversidedebugger`.`DebugCallStack`.`DebugSessionId` = pDebugSessionId ) and ( `serversidedebugger`.`DebugCallStack`.`Id` = nextId ));
    return returnValue;

end //

CREATE PROCEDURE `CleanupScope`( pDebugSessionId int )
begin
  
  delete from `serversidedebugger`.`DebugScope` 
	where ( `serversidedebugger`.`DebugScope`.`DebugSessionId` = pDebugSessionId ) and 
		( `serversidedebugger`.`DebugScope`.`DebugScopeLevel` = (select `serversidedebugger`.`debugdata`.`Val` from `serversidedebugger`.`debugdata` where `serversidedebugger`.`debugdata`.`id` = 1 limit 1 ) );
  update `serversidedebugger`.`DebugData` set `serversidedebugger`.`DebugData`.`Val` = `serversidedebugger`.`DebugData`.`Val` - 1 
	where `serversidedebugger`.`DebugData`.`Id` = 1;

end //


CREATE PROCEDURE `DumpScopeVar`( pDebugSessionId int, pDebugScopeLevel int, pVarName varchar( 30 ), pVarValue binary )
begin
  
  replace `serversidedebugger`.`DebugScope`( DebugSessionId, DebugScopeLevel, VarName, VarValue ) values ( pDebugSessionId, pDebugScopeLevel, pVarName, pVarValue );
  
end //


CREATE PROCEDURE `ExitEnterCriticalSection`( spName varchar( 30 ), lineNumber int )
begin
    
  declare gblNetWriteTimeout int;
  declare garbage int;

  set @@global.net_write_timeout = 999998;
  set garbage = ( select release_lock( 'lock1' ) );
  repeat 
	set gblNetWriteTimeout = @@global.net_write_timeout;
  until gblNetWriteTimeout <> 999998 end repeat;
  set garbage = (select get_lock( 'lock1', 999999 ) );

end //


CREATE PROCEDURE `Pop`( pDebugSessionId int )
begin

    declare nextId int;
    set nextId = ( select max( `serversidedebugger`.`DebugCallStack`.`Id` ) from `serversidedebugger`.`DebugCallStack` 
		where `serversidedebugger`.`DebugCallStack`.`DebugSessionId` = pDebugSessionId );
    delete from `serversidedebugger`.`DebugCallStack` where ( `serversidedebugger`.`DebugCallStack`.`DebugSessionId` = pDebugSessionId ) 
		and ( `serversidedebugger`.`DebugCallStack`.`Id` = nextId );

end //


CREATE PROCEDURE `Push`( pDebugSessionId int, pRoutineName varchar( 50 ) )
begin

    insert into `serversidedebugger`.`DebugCallStack`( DebugSessionId, RoutineName ) values ( pDebugSessionId, pRoutineName );

end //

create procedure SetDebugScopeVar( pDebugSessionId int, pDebugScopeLevel int, pVarName varchar( 30 ), pVarValue varbinary( 50000 ) )
begin

	insert into `serversidedebugger`.`DebugScope`( DebugSessionId, DebugScopeLevel, VarName, VarValue ) values ( pDebugSessionId, pDebugScopeLevel, pVarName, pVarValue );

end //

create procedure GetDebugScopeVar( pDebugSessionId int, pDebugScopeLevel int, pVarName varchar( 30 ), out pVarValue varbinary( 50000 ))
begin
	
	declare pId int;
	
	set pId = ( select max( Id ) from `serversidedebugger`.`DebugScope` 
		where ( DebugSessionId = pDebugSessionId ) and ( DebugScopeLevel = pDebugScopeLevel ) and ( VarName = pVarName ) );
	select pVarValue = VarValue from `serversidedebugger`.`DebugScope` 
		where ( DebugSessionId = pDebugSessionId ) and ( DebugScopeLevel = pDebugScopeLevel ) and ( VarName = pVarName ) and ( Id = pId );
	
end //
