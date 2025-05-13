--DATABASE CREATION
use master
IF EXISTS (SELECT * FROM sys.databases WHERE name = 'EventSystemDB')
DROP DATABASE EventSystemDB
CREATE DATABASE EventSystemDB
use EventSystemDB

--TABLE CREATION SECTION

CREATE TABLE Venue (
VenueID INT IDENTITY(1,1) PRIMARY KEY NOT NULL,
VenueName VARCHAR(50) NOT NULL,
[Location] VARCHAR(50) NOT NULL,
Capacity INT NOT NULL,
ImageUrl VARCHAR(MAX) NOT NULL
);


CREATE TABLE [Event](
EventID INT IDENTITY(1,1) PRIMARY KEY NOT NULL,
EventName VARCHAR(50) NOT NULL,
EventDate DATE NOT NULL,
Description VARCHAR(100) NOT NULL
);

CREATE TABLE Booking(
BookingID INT IDENTITY(1,1) PRIMARY KEY NOT NULL,
VenueID INT  FOREIGN KEY REFERENCES Venue(VenueID),
EventID INT  FOREIGN KEY REFERENCES Event(EventID),
BookingDate DATE NOT NULL,
);


--TABLE ALTERARTION SECTION

--TABLE INSERTION SECTION

INSERT INTO Venue (VenueName,[Location],Capacity,ImageUrl)
VALUES ('Nasrec','Joburg',500,'jj')


INSERT INTO [Event] (EventName,EventDate,[Description])
VALUES ('MFest','2023-12-12','bmw car festival')

INSERT INTO Booking (VenueID,EventID,BookingDate)
VALUES (1,1,'2023-09-15')





--TABLE MANIPLULATION SECTION

SELECT * FROM Venue
SELECT * FROM [Event] 
SELECT * FROM Booking

SELECT 
    b.BookingID,
    v.VenueName,
    v.[Location],
    e.EventName,
    e.EventDate,
    b.BookingDate
FROM Booking b
JOIN Venue v ON b.VenueID = v.VenueID
JOIN [Event] e ON b.EventID = e.EventID;

GO

CREATE VIEW BookingView AS
SELECT 
    b.BookingID,
    v.VenueName,
    v.[Location],
    e.EventName,
    e.EventDate,
    b.BookingDate
FROM Booking b
JOIN Venue v ON b.VenueID = v.VenueID
JOIN [Event] e ON b.EventID = e.EventID;
;

GO

SELECT * FROM BookingView

--STORED PROCEDURE SECTION