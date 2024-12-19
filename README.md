import ldap
from django_auth_ldap.config import LDAPSearch

# LDAP Server URI
AUTH_LDAP_SERVER_URI = "ldap://10.172.170.154"  # Your LDAP server address

# Bind credentials
AUTH_LDAP_BIND_DN = "cn=administrator,ou=KAWC USERS,dc=KAWC,dc=org"  # Your admin DN
AUTH_LDAP_BIND_PASSWORD = "PasswOrd@123"                              # Admin password

# User search
AUTH_LDAP_USER_SEARCH = LDAPSearch(
    "ou=KAWC USERS,dc=KAWC,dc=org",  # Base DN for searching users
    ldap.SCOPE_SUBTREE,
    "(sAMAccountName=%(user)s)"      # Match user using sAMAccountName attribute
)

# Map LDAP attributes to Django user fields
AUTH_LDAP_USER_ATTR_MAP = {
    "first_name": "givenName",
    "last_name": "sn",
    "email": "mail",
}

# Enable authentication backends
AUTHENTICATION_BACKENDS = [
    "django_auth_ldap.backend.LDAPBackend",       # LDAP authentication backend
    "django.contrib.auth.backends.ModelBackend",  # Default Django authentication backend
]

# Optional: Debugging (useful for diagnosing issues)
import logging
logger = logging.getLogger("django_auth_ldap")
logger.addHandler(logging.StreamHandler())
logger.setLevel(logging.DEBUG)
