INSERT INTO `recordshiftenergy` 
( `State`,`Group`, `MeterType`,`Year`,`Month`,`Day`,`Week`,`MeterName`,`Shift`,`Consume`,`ProductVolume`,`RateToCarbon`,`RateToCost`)
VALUES
( 2,"Takeup","Power",2023,5,3,18,"TakeupPower","1",2.1,56,0.548,0.6),
( 2,"Takeup","Power",2023,5,3,18,"TakeupPower","2",5,56,0.548,0.6),
( 2,"Takeup","Power",2023,5,3,18,"TakeupPower","3",96.3,56,0.548,0.6),
( 2,"Takeup","Power",2023,5,4,18,"TakeupPower","1",12.1,56,0.548,0.6),
( 2,"Takeup","Power",2023,5,4,18,"TakeupPower","2",15,56,0.548,0.6),
( 2,"Takeup","Power",2023,5,4,18,"TakeupPower","3",9.3,56,0.548,0.6),
( 2,"Takeup","Power",2023,4,30,18,"TakeupPower","1",12.1,56,0.548,0.6),
( 2,"Takeup","Power",2023,4,30,18,"TakeupPower","2",15,56,0.548,0.6),
( 2,"Takeup","Power",2023,4,30,18,"TakeupPower","3",9.3,56,0.548,0.6),
( 2,"Takeup","Power",2023,5,1,18,"TakeupPower","1",12.1,56,0.548,0.6),
( 2,"Takeup","Power",2023,5,1,18,"TakeupPower","2",15,56,0.548,0.6),
( 2,"Takeup","Power",2023,5,1,18,"TakeupPower","3",9.3,56,0.548,0.6),
( 2,"Takeup","Power",2023,5,2,18,"TakeupPower","1",12.1,56,0.548,0.6),
( 2,"Takeup","Power",2023,5,2,18,"TakeupPower","2",15,56,0.548,0.6),
( 2,"Takeup","Power",2023,5,2,18,"TakeupPower","3",9.3,56,0.548,0.6),
( 2,"Takeup","Power",2023,5,5,18,"TakeupPower","1",2.1,56,0.548,0.6),
( 2,"Takeup","Power",2023,5,5,18,"TakeupPower","2",5,56,0.548,0.6),
( 2,"Takeup","Power",2023,5,5,18,"TakeupPower","3",96.3,56,0.548,0.6),

INSERT INTO `recordmultirateenergy` 
( `State`,`Group`, `MeterType`,`Year`,`Month`,`Day`,`Week`,`MeterName`,`RateDuration`,`Consume`,`ProductVolume`,`RateToCarbon`,`RateToCost`)
VALUES
( 2,"Takeup","Power",2023,5,1,18,"TakeupPower","10",15.3,47,0.548,0.4),
( 2,"Takeup","Power",2023,5,1,18,"TakeupPower","20",13.1,53,0.548,0.6),
( 2,"Takeup","Power",2023,5,1,18,"TakeupPower","21",15,60,0.548,0.6),
( 2,"Takeup","Power",2023,5,1,18,"TakeupPower","22",2.3,45,0.548,0.6),
( 2,"Takeup","Power",2023,5,1,18,"TakeupPower","30",11.1,50,0.548,0.8),
( 2,"Takeup","Power",2023,5,1,18,"TakeupPower","31",12,59,0.548,0.8),
( 2,"Takeup","Power",2023,5,1,18,"TakeupPower","40",15.3,52,0.548,1),
( 2,"Takeup","Power",2023,5,1,18,"TakeupPower","41",12.1,53,0.548,1),
( 2,"Takeup","Power",2023,5,2,18,"TakeupPower","10",15.3,47,0.548,0.4),
( 2,"Takeup","Power",2023,5,2,18,"TakeupPower","20",13.1,53,0.548,0.6),
( 2,"Takeup","Power",2023,5,2,18,"TakeupPower","21",15,60,0.548,0.6),
( 2,"Takeup","Power",2023,5,2,18,"TakeupPower","22",2.3,45,0.548,0.6),
( 2,"Takeup","Power",2023,5,2,18,"TakeupPower","30",11.1,50,0.548,0.8),
( 2,"Takeup","Power",2023,5,2,18,"TakeupPower","31",12,59,0.548,0.8),
( 2,"Takeup","Power",2023,5,2,18,"TakeupPower","40",15.3,52,0.548,1),
( 2,"Takeup","Power",2023,5,2,18,"TakeupPower","41",12.1,53,0.548,1)


SELECT  `Shift`,SUM(`Consume`*``)
FROM `RecordShiftEnergy`
Where `Group` = "Furnace" and `Year` = 2023 and `Month` = 5 and `Day` = 5
Group by `Shift`
SELECT Shift,SUM(Consume) FROM RecordShiftEnergy WHERE Group={0:sql_literal} AND Year=2023 AND Month=5 AND Day=5 GROUP BY Shift
{0:sql_identifier}: Shift
1:Consume
{0:sql_literal}