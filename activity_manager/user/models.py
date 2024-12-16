# Create your models here.
from django.db import models
from django.contrib.auth.models import User


class UserProfile(models.Model):
    user = models.OneToOneField(User, on_delete=models.CASCADE, related_name="profile")
    company = models.CharField(max_length=255, blank=True, null=True)
    division = models.CharField(max_length=255, blank=True, null=True)
    unit_group = models.CharField(max_length=255, blank=True, null=True)
    office_location = models.CharField(max_length=255, blank=True, null=True)
    supervisor = models.CharField(max_length=255, blank=True, null=True)
    hide_business_mobile = models.BooleanField(default=False)
    business_mobile = models.CharField(max_length=20, blank=True, null=True)
    otp_mobile = models.CharField(max_length=20, blank=True, null=True)
    telephone = models.CharField(max_length=20, blank=True, null=True)

    def __str__(self):
        return f"{self.user.username}'s Profile"



# from django_auth_ldap.backend import populate_user

# def ldap_user_populate(sender, user, ldap_user, **kwargs):
#     profile, created = UserProfile.objects.get_or_create(user=user)
#     profile.company = ldap_user.attrs.get('company', [None])[0]
#     profile.division = ldap_user.attrs.get('division', [None])[0]
#     profile.unit_group = ldap_user.attrs.get('unitGroup', [None])[0]
#     profile.office_location = ldap_user.attrs.get('physicalDeliveryOfficeName', [None])[0]
#     profile.supervisor = ldap_user.attrs.get('manager', [None])[0]
#     profile.business_mobile = ldap_user.attrs.get('mobile', [None])[0]
#     profile.telephone = ldap_user.attrs.get('telephoneNumber', [None])[0]
#     profile.otp_mobile = ldap_user.attrs.get('otpMobile', [None])[0]
#     profile.save()

# from django_auth_ldap.backend import populate_user
# populate_user.connect(ldap_user_populate)
