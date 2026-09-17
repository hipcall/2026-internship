---
title: "Hipcall API anahtarı nasıl alınır ve ilk istek nasıl gönderilir?"
description: "Panelden API anahtarı oluşturun, kimlik doğrulamalı ilk isteğinizi gönderin ve dönen cevabı inceleyin."
slug: hipcall-api-anahtari-nasil-alinir
lang: tr
locales: [en, tr]
pubDate: 2026-09-16
categories: [developers]
intent: informational
translationKey: how-to-get-a-hipcall-api-key
tags: [api, authentication, getting-started]
authors: [hipcall-team]
featured: false
draft: true
task: 01
status: draft
---

## Genel bakış

Bu rehberde HipCall API anahtarı oluşturmayı ve profil endpoint'ine kimlik doğrulamalı ilk isteğinizi göndermeyi öğreneceksiniz. Ayrıca başarılı bir cevabın nasıl göründüğünü ve yaygın kimlik doğrulama hatalarını nasıl ele alacağınızı göreceksiniz.

## Başlamadan önce

API anahtarı oluşturmak için **Yönetici** veya **Kurucu** rolüne sahip bir kullanıcı hesabınız olmalıdır. **Standart** ve **Yetkili** rollerine sahip kullanıcılar geliştirici ayarlarına erişemez.

1. `https://use.hipcall.com.tr/` adresinden HipCall paneline giriş yapın.
2. **Ayarlar > Geliştirici** menüsünü açın.
3. **API** bölümünü açın.
4. Yeni bir API anahtarı oluşturmak için **Yeni** butonuna tıklayın.
5. **Ad** ve **Son geçerlilik tarihi** alanlarını doldurun. Her iki alan da zorunludur.
6. API anahtarını oluşturun ve gösterilen gizli anahtarın tamamını güvenli bir yerde saklayın.

Varsayılan geçerlilik süresi bir yıldır. Son geçerlilik tarihini en fazla üç yıl sonrasına kadar seçebilirsiniz.

API anahtarı oluşturulduktan sonra HipCall gizli anahtarın tamamını ekranda gösterir. Sayfadan ayrılmadan önce anahtarı güvenli bir yerde saklayın. Sayfayı kapattıktan sonra anahtarın tamamını tekrar görüntüleyemezsiniz. API anahtarı listesinde anahtarın maskelenmiş hâli gösterilir.

Anahtarı kaybederseniz mevcut anahtarı silip yeni bir API anahtarı oluşturun.

## İlk isteğinizi gönderin

API anahtarını doğrudan komutun içine yazmak yerine bir ortam değişkeninde saklayın.

```bash
export HIPCALL_API_TOKEN="..."
```

Türkiye DEMO hesabı için profil endpoint'i aşağıdaki adrestedir:

```text
https://use.hipcall.com.tr/api/v3/profile
```

API anahtarını `Authorization` başlığında Bearer token olarak göndererek `GET` isteği oluşturun.

```bash
curl -sS -i \
  -H "Authorization: Bearer $HIPCALL_API_TOKEN" \
  https://use.hipcall.com.tr/api/v3/profile
```

Panelde görüntülenen API anahtarını doğrudan kullanabilirsiniz. `SFMyNTY.` ön ekli anahtar biçimi de desteklenir; bu nedenle anahtar üzerinde ek bir dönüşüm yapmanız gerekmez.

## Başarılı cevap

Geçerli bir API anahtarıyla gönderilen istek `200 OK` cevabı döndürür.

Cevap, `data` nesnesi altında kimliği doğrulanan kullanıcı, izinler, yetenekler, hesap, roller ve abonelik bilgileri gibi alanları içerir.

```json
{
  "data": {
    "user": {
      "id": 4200,
      "owner": true,
      "suspended": false,
      "state": "available",
      "title": null,
      "email": "ornek@firma.com",
      "locale": "tr_TR",
      "timezone": "Europe/Istanbul",
      "created_at": "2026-07-21T10:55:30",
      "full_name": "Ahmet Y.",
      "numbers": [
        {
          "id": 939,
          "name": "Sub Manager",
          "number": "+90850XXXXXXX",
          "country": "TR"
        },
        {
          "id": 938,
          "name": "Super Manager",
          "number": "+90850XXXXXXX",
          "country": "TR"
        }
      ],
      "default_number": {
        "id": 938,
        "name": "Super Manager",
        "number": "+90850XXXXXXX",
        "country": "TR"
      },
      "first_name": "Ahmet",
      "last_logged_in": "2026-09-15T13:10:07",
      "last_name": "Y.",
      "phone_countries": [
        "AR",
        "TR"
      ],
      "phone_prefix": "TR",
      "avatar_url": null
    },
    "account": {
      "locale": "tr_TR",
      "timezone": "Europe/Istanbul",
      "created_at": "2026-07-21T10:55:30",
      "contact_center": {
        "b2b": true
      },
      "limits": {
        "max_channel": 5
      },
      "slug": null
    },
    "roles": []
  }
}
```

Yayınlamadan önce kişisel ve hesap bilgilerini maskeleyin.

Cevap başlıklarında rate limit bilgileri de bulunur. Test edilen DEMO ortamında limit **dakikada 60 istek** olarak dönmektedir.

## Hata aldığınızda

### API anahtarı gönderilmediğinde

`Authorization` başlığı olmadan gönderilen istek `401 Unauthorized` cevabı döndürür.

```bash
curl -sS -i \
  https://use.hipcall.com.tr/api/v3/profile
```

Cevap gövdesi:

```json
{
  "errors": {
    "detail": "Authentication required. Provide either OAuth Bearer token or API token."
  }
}
```

### Geçersiz veya süresi dolmuş API anahtarında

Geçersiz veya süresi dolmuş bir API anahtarı da `401 Unauthorized` cevabı döndürür. Ancak cevap gövdesi farklıdır.

```bash
curl -sS -i \
  -H "Authorization: Bearer bu-anahtar-sahte" \
  https://use.hipcall.com.tr/api/v3/profile
```

Cevap gövdesi:

```json
{
  "errors": {
    "detail": "Not authorized. Check your API token is valid and not expired."
  }
}
```

### API adresi

Türkiye DEMO hesaplarında API adresi olarak `https://use.hipcall.com.tr` kullanın.

Profil endpoint'i:

```text
https://use.hipcall.com.tr/api/v3/profile
```

## API anahtarınızı güvenli tutun

API anahtarınızı repository, forum gönderisi, ekran görüntüsü veya blog yazısında paylaşmayın. API anahtarlarını, telefon numaralarını, e-posta adreslerini ve isimleri paylaşmadan önce maskeleyin.

Gizli anahtarın tamamı oluşturulduğu sırada gösterilir ve sayfadan ayrıldıktan sonra tekrar görüntülenemez.

Anahtarınız kaybolursa veya açığa çıkarsa anahtarı silin ve yeni bir API anahtarı oluşturun.

## Parametre listesi

Profil endpoint'i bir istek gövdesi gerektirmez. Kimlik doğrulama `Authorization` başlığı üzerinden sağlanır.

| Parametre       | Tip    | Zorunlu  | Açıklama                                       |
| --------------- | ------ | -------- | ---------------------------------------------- |
| `Authorization` | header | evet     | İsteğin kimlik doğrulaması için kullanılan Bearer token. |

## Sonraki adımlar

Diğer endpoint'leri incelemek için [HipCall API Referansı](https://use.hipcall.com/api-docs/) sayfasını ziyaret edin.

Sorular sormak veya entegrasyon deneyiminizi paylaşmak için [HipCall Community](https://community.hipcall.com/) sayfasına gidin.
