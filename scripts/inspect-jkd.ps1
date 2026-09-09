param([Parameter(Mandatory = $true)][string]$Path)

$connection = New-Object System.Data.OleDb.OleDbConnection(
    "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=$Path")
$connection.Open()
try {
    $connection.GetSchema('Columns', @($null, $null, 'Person', $null)) |
        Sort-Object ORDINAL_POSITION |
        Select-Object COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, ORDINAL_POSITION |
        Format-Table -AutoSize

    $command = $connection.CreateCommand()
    $command.CommandText = 'SELECT COUNT(*) FROM Person'
    "COUNT=$($command.ExecuteScalar())"

    $command.CommandText = 'SELECT TOP 5 * FROM Person'
    $adapter = New-Object System.Data.OleDb.OleDbDataAdapter($command)
    $table = New-Object System.Data.DataTable
    [void]$adapter.Fill($table)
    $table | Format-List *
}
finally {
    $connection.Close()
}
