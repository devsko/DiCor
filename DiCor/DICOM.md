|
:---: | ---
(=) | The value of the parameter is equal to the value of the parameter in the column to the left.
\- | Not applicable. The parameter shall not be present.
C | The parameter is conditional. The condition(s) are defined by the text that describes the parameter.
M | Mandatory usage
MF | Mandatory with a fixed value
U | The use of this parameter is a DIMSE Service User option
UF | User Option with a fixed value

# Upper Layer Services
## A-ASSOCIATE (Confirmed)
A-ASSOCIATE parameter name | Request | Indication | Response | Confirmation
--- | :---: | :---: | :---: | :---:
application context name | M | M(=) | M | M(=)
calling AE title | M | M(=) | M | M(=)
called AE title | M | M(=) | M | M(=)
user information | M | M(=) | M | M(=)
result | | | M | M(=)
result source | | | | M
diagnostic | | | U | C(=)
calling presentation address | M | M(=) | |
called presentation address | M | M(=) | |
presentation context definition list | M | M(=) | |
presentation context definition list result | | | M | M(=)

## A-RELEASE Confirmed
## A-ABORT Non-Confirmed
## A-P-ABORT Provider-initiated
## P-DATA Non-Confirmed