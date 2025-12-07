EXEC pCountry;4 @id = 5


alter proc pCountry;4
@id int
AS
BEGIN
    DELETE Country 
    WHERE id = @id
END
GO