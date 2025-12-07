#!/bin/bash

# Test authentication with the JWT token

TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiJkNGU0Y2U3OC0wZTQ2LTQzMjctYTg4NS05ZmEyNTI5NGQ5NDMiLCJuYW1lIjoiQWRtaW5Vc2VyIiwic3ViIjoiZDRlNGNlNzgtMGU0Ni00MzI3LWE4ODUtOWZhMjUyOTRkOTQzIiwic2lkIjoiZDRlNGNlNzgtMGU0Ni00MzI3LWE4ODUtOWZhMjUyOTRkOTQzIiwidW5pcXVlX25hbWUiOiJBZG1pblVzZXIiLCJlbWFpbCI6ImFkbWluQGNsZWFuaHIuY29tIiwiZ2l2ZW5fbmFtZSI6IkFkbWluVXNlciIsImp0aSI6ImI4MWUwZThkLTM3ODQtNGE4My1hYWY0LWE0ZWY3YTQyNDVlZSIsImlhdCI6IjIwMjUtMTItMDdUMTQ6MjA6MjVaIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjoiQWRtaW4iLCJuYmYiOjE3NjUwOTIwMjUsImV4cCI6MTc2NTE3ODQyNSwiaXNzIjoiU2FtcGxlSWRlbnRpdHkuY29tIiwiYXVkIjoiU2FtcGxlSWRlbnRpdHkuY29tIn0.1WvgcGuCQS0PAhDBMRTyuNQeJUyc8LIw9xyaaW9Erqk"

echo "Testing logout endpoint with Bearer token..."
echo ""

curl -v -X POST http://localhost:5100/api/v1/user/logout \
  -H "accept: text/plain" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"test-refresh-token"}'

echo ""
echo ""
echo "Done!"
